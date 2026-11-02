using FluentValidation.Results;
using MiCash.Communication.Requests.PushDevices;
using MiCash.Communication.Responses.PushDevices;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.PushNotifications;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Domain.Services.PushNotifications;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging;

namespace MiCash.Application.UseCases.PushDevices
{
    public interface IRegisterPushDeviceUseCase
    {
        Task<ResponseRegisteredPushDeviceJson> Execute ( RequestRegisterPushDeviceJson request , CancellationToken cancellationToken );
    }

    public interface IDeactivatePushDeviceUseCase
    {
        Task Execute ( RequestDeactivatePushDeviceJson request , CancellationToken cancellationToken );
    }

    public interface IGetPushDevicesUseCase
    {
        Task<ResponsePushDevicesJson> Execute ( CancellationToken cancellationToken );
    }

    public interface ISendTestPushNotificationUseCase
    {
        Task<ResponseTestPushNotificationJson> Execute ( CancellationToken cancellationToken );
    }

    /// <summary>
    /// Upsert by token for the authenticated user. A token already registered by another user is
    /// transferred (same phone, different account): the previous account stops receiving pushes there.
    /// </summary>
    public class RegisterPushDeviceUseCase : IRegisterPushDeviceUseCase
    {
        public const int MAX_ATTEMPTS = 2;

        private readonly IPushDeviceUpdateOnlyRepository _updateOnlyRepository;
        private readonly IPushDeviceWriteOnlyRepository _writeOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<RegisterPushDeviceUseCase> _logger;

        public RegisterPushDeviceUseCase (
            IPushDeviceUpdateOnlyRepository updateOnlyRepository ,
            IPushDeviceWriteOnlyRepository writeOnlyRepository ,
            ILoggedUser loggedUser ,
            IUnitOfWork unitOfWork ,
            TimeProvider timeProvider ,
            ILogger<RegisterPushDeviceUseCase> logger )
        {
            _updateOnlyRepository = updateOnlyRepository;
            _writeOnlyRepository = writeOnlyRepository;
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<ResponseRegisteredPushDeviceJson> Execute ( RequestRegisterPushDeviceJson request , CancellationToken cancellationToken )
        {
            PushDeviceMapper.ThrowIfInvalid(new ValidatorRegisterPushDevice().Validate(request));

            var user = await _loggedUser.Get();

            // Two concurrent registrations of the same new token: the loser hits the unique index
            // (ConflictException) and retries as an update of the winner's row.
            for ( var attempt = 1; ; attempt++ ) {
                try {
                    return await TryRegister(user.Id , request , cancellationToken);
                } catch ( ConflictException ) when ( attempt < MAX_ATTEMPTS ) {
                }
            }
        }

        private async Task<ResponseRegisteredPushDeviceJson> TryRegister (
            long userId , RequestRegisterPushDeviceJson request , CancellationToken cancellationToken )
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var token = request.ExpoPushToken;
            var deviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? null : request.DeviceId.Trim();
            var deviceName = string.IsNullOrWhiteSpace(request.DeviceName) ? null : request.DeviceName.Trim();

            var device = await _updateOnlyRepository.GetByTokenAsync(token , cancellationToken);

            if ( device is null ) {
                device = new PushDevice { UserId = userId , ExpoPushToken = token , CreatedAt = now };

                await _writeOnlyRepository.AddAsync(device , cancellationToken);
            } else if ( device.UserId != userId ) {
                _logger.LogInformation(
                    "Push token {TokenSuffix} transferido do usuário {PreviousUserId} para {UserId}." ,
                    PushTokenMask.Suffix(token) , device.UserId , userId);

                device.UserId = userId;
                device.LastUsedAt = null;
            }

            device.Platform = PushDeviceRules.ParsePlatform(request.Platform)!.Value;
            device.DeviceId = deviceId;
            device.DeviceName = deviceName;
            device.IsActive = true;
            device.UpdatedAt = now;

            // Same install with a new token (Expo rotated it): the old tokens are no longer used.
            if ( deviceId is not null ) {
                var older = await _updateOnlyRepository.GetActiveByDeviceIdAsync(userId , deviceId , cancellationToken);

                foreach ( var old in older.Where(d => d.ExpoPushToken != token) ) {
                    old.IsActive = false;
                    old.UpdatedAt = now;
                }
            }

            await _unitOfWork.Commit(cancellationToken);

            return new ResponseRegisteredPushDeviceJson { Id = device.Id };
        }
    }

    public class DeactivatePushDeviceUseCase : IDeactivatePushDeviceUseCase
    {
        private readonly IPushDeviceUpdateOnlyRepository _updateOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _timeProvider;

        public DeactivatePushDeviceUseCase (
            IPushDeviceUpdateOnlyRepository updateOnlyRepository ,
            ILoggedUser loggedUser ,
            IUnitOfWork unitOfWork ,
            TimeProvider timeProvider )
        {
            _updateOnlyRepository = updateOnlyRepository;
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
        }

        public async Task Execute ( RequestDeactivatePushDeviceJson request , CancellationToken cancellationToken )
        {
            PushDeviceMapper.ThrowIfInvalid(new ValidatorDeactivatePushDevice().Validate(request));

            var user = await _loggedUser.Get();

            var device = await _updateOnlyRepository.GetByTokenAsync(request.ExpoPushToken , cancellationToken);

            // Another user's token gets the same answer as an unknown one: nothing to learn from it.
            if ( device is null || device.UserId != user.Id ) {
                throw new NotFoundException(ResourceErrorMessages.PUSH_DEVICE_NOT_FOUND);
            }

            if ( device.IsActive == false ) {
                return;
            }

            device.IsActive = false;
            device.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

            await _unitOfWork.Commit(cancellationToken);
        }
    }

    public class GetPushDevicesUseCase : IGetPushDevicesUseCase
    {
        private readonly IPushDeviceReadOnlyRepository _readOnlyRepository;
        private readonly ILoggedUser _loggedUser;

        public GetPushDevicesUseCase ( IPushDeviceReadOnlyRepository readOnlyRepository , ILoggedUser loggedUser )
        {
            _readOnlyRepository = readOnlyRepository;
            _loggedUser = loggedUser;
        }

        public async Task<ResponsePushDevicesJson> Execute ( CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var devices = await _readOnlyRepository.GetAllByUserAsync(user.Id , cancellationToken);

            return new ResponsePushDevicesJson
            {
                Devices = devices.Select(device => new ResponsePushDeviceJson
                {
                    Id = device.Id ,
                    Platform = PushDeviceRules.ToJson(device.Platform) ,
                    DeviceName = device.DeviceName ,
                    TokenSuffix = PushTokenMask.Suffix(device.ExpoPushToken) ,
                    IsActive = device.IsActive ,
                    CreatedAt = device.CreatedAt ,
                    LastUsedAt = device.LastUsedAt
                }).ToList()
            };
        }
    }

    /// <summary>Sends a test notification to the authenticated user's own devices (exposed only in Development).</summary>
    public class SendTestPushNotificationUseCase : ISendTestPushNotificationUseCase
    {
        public const string TYPE = "test";

        private readonly IPushNotificationService _pushNotificationService;
        private readonly IPushDeviceReadOnlyRepository _deviceReadOnlyRepository;
        private readonly ILoggedUser _loggedUser;

        public SendTestPushNotificationUseCase (
            IPushNotificationService pushNotificationService ,
            IPushDeviceReadOnlyRepository deviceReadOnlyRepository ,
            ILoggedUser loggedUser )
        {
            _pushNotificationService = pushNotificationService;
            _deviceReadOnlyRepository = deviceReadOnlyRepository;
            _loggedUser = loggedUser;
        }

        public async Task<ResponseTestPushNotificationJson> Execute ( CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            // A delivery test: without a device it is refused before anything is recorded in the center.
            if ( (await _deviceReadOnlyRepository.GetActiveByUserAsync(user.Id , cancellationToken)).Count == 0 ) {
                throw new ErrorOnValidationException([ResourceErrorMessages.PUSH_NO_ACTIVE_DEVICES]);
            }

            var result = await _pushNotificationService.EnqueueAsync(
                user.Id ,
                new PushMessage(
                    Type: TYPE ,
                    Title: "Notificações ativadas" ,
                    Body: "Este é um teste: as notificações do MiCash estão funcionando neste aparelho." ,
                    Route: "/home") ,
                cancellationToken)
                ?? throw new ErrorOnValidationException([ResourceErrorMessages.PUSH_NO_ACTIVE_DEVICES]);

            return new ResponseTestPushNotificationJson { NotificationId = result.NotificationId , Devices = result.Devices };
        }
    }

    internal static class PushDeviceMapper
    {
        public static void ThrowIfInvalid ( ValidationResult result )
        {
            if ( result.IsValid == false ) {
                throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
            }
        }
    }
}
