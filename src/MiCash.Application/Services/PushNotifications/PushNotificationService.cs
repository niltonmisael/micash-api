using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.PushNotifications;
using MiCash.Domain.Services.Jobs;
using MiCash.Domain.Services.PushNotifications;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MiCash.Application.Services.PushNotifications
{
    /// <summary>
    /// Entry point for any rule that wants to notify a user. Knows nothing about expenses, goals etc.:
    /// it records the notification (always: notification center; one delivery per active device) and lets the jobs send it.
    /// </summary>
    public sealed class PushNotificationService : IPushNotificationService
    {
        public const int TYPE_MAX_LENGTH = 50;
        public const int TITLE_MAX_LENGTH = 150;
        public const int BODY_MAX_LENGTH = 1000;
        public const int IDEMPOTENCY_KEY_MAX_LENGTH = 200;

        private readonly IPushNotificationReadOnlyRepository _notificationReadOnlyRepository;
        private readonly IPushNotificationWriteOnlyRepository _notificationWriteOnlyRepository;
        private readonly IPushDeviceReadOnlyRepository _deviceReadOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<PushNotificationService> _logger;

        public PushNotificationService (
            IPushNotificationReadOnlyRepository notificationReadOnlyRepository ,
            IPushNotificationWriteOnlyRepository notificationWriteOnlyRepository ,
            IPushDeviceReadOnlyRepository deviceReadOnlyRepository ,
            IUnitOfWork unitOfWork ,
            IBackgroundJobService backgroundJobService ,
            TimeProvider timeProvider ,
            ILogger<PushNotificationService> logger )
        {
            _notificationReadOnlyRepository = notificationReadOnlyRepository;
            _notificationWriteOnlyRepository = notificationWriteOnlyRepository;
            _deviceReadOnlyRepository = deviceReadOnlyRepository;
            _unitOfWork = unitOfWork;
            _backgroundJobService = backgroundJobService;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<PushEnqueueResult?> EnqueueAsync ( long userId , PushMessage message , CancellationToken cancellationToken )
        {
            Validate(message);

            if ( message.IdempotencyKey is not null &&
                await _notificationReadOnlyRepository.ExistsAsync(userId , message.IdempotencyKey , cancellationToken) ) {
                _logger.LogInformation(
                    "Push {Type} para o usuário {UserId} ignorado: chave de idempotência já usada." , message.Type , userId);

                return null;
            }

            // The notification is always recorded (notification center); only the push depends on active devices.
            var devices = await _deviceReadOnlyRepository.GetActiveByUserAsync(userId , cancellationToken);

            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var notification = new PushNotification
            {
                UserId = userId ,
                Type = message.Type ,
                Title = message.Title ,
                Body = message.Body ,
                Data = JsonSerializer.Serialize(BuildData(message)) ,
                IdempotencyKey = message.IdempotencyKey ,
                CreatedAt = now ,
                Deliveries = devices.Select(device => new PushNotificationDelivery
                {
                    DeviceId = device.Id ,
                    NextAttemptAt = now ,
                    CreatedAt = now ,
                    UpdatedAt = now
                }).ToList()
            };

            await _notificationWriteOnlyRepository.AddAsync(notification , cancellationToken);

            try {
                await _unitOfWork.Commit(cancellationToken);
            } catch ( ConflictException ) when ( message.IdempotencyKey is not null ) {
                // Same idempotency key committed concurrently: the other one wins.
                return null;
            }

            if ( devices.Count == 0 ) {
                _logger.LogInformation(
                    "Notificação {NotificationId} ({Type}) gravada só na central do usuário {UserId}: nenhum aparelho ativo." ,
                    notification.Id , message.Type , userId);

                return new PushEnqueueResult(notification.Id , 0);
            }

            try {
                _backgroundJobService.EnqueuePushNotificationDispatch(notification.Id);
            } catch ( System.Exception ex ) {
                // The recurring dispatch job sends it within a minute anyway.
                _logger.LogWarning(ex , "Não foi possível agendar o envio imediato do push {NotificationId}." , notification.Id);
            }

            _logger.LogInformation(
                "Push {NotificationId} ({Type}) enfileirado para o usuário {UserId} em {Devices} aparelho(s)." ,
                notification.Id , message.Type , userId , devices.Count);

            return new PushEnqueueResult(notification.Id , devices.Count);
        }

        /// <summary>Expo "data": the app opens data.url on tap; type lets it tell notifications apart.</summary>
        private static Dictionary<string , string> BuildData ( PushMessage message )
        {
            var data = new Dictionary<string , string> { ["type"] = message.Type };

            if ( message.Route is not null ) {
                data["url"] = message.Route;
            }

            return data;
        }

        private static void Validate ( PushMessage message )
        {
            if ( string.IsNullOrWhiteSpace(message.Type) || message.Type.Length > TYPE_MAX_LENGTH ) {
                throw new ArgumentException("Invalid push type." , nameof(message));
            }

            if ( string.IsNullOrWhiteSpace(message.Title) || message.Title.Length > TITLE_MAX_LENGTH ) {
                throw new ArgumentException("Invalid push title." , nameof(message));
            }

            if ( string.IsNullOrWhiteSpace(message.Body) || message.Body.Length > BODY_MAX_LENGTH ) {
                throw new ArgumentException("Invalid push body." , nameof(message));
            }

            if ( message.Route is not null && !PushRoutes.IsAllowed(message.Route) ) {
                throw new ArgumentException($"Route '{message.Route}' is not an allowed app route." , nameof(message));
            }

            if ( message.IdempotencyKey is { } key && (string.IsNullOrWhiteSpace(key) || key.Length > IDEMPOTENCY_KEY_MAX_LENGTH) ) {
                throw new ArgumentException("Invalid idempotency key." , nameof(message));
            }
        }
    }
}
