using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.PushNotifications;
using MiCash.Domain.Services.Jobs;
using MiCash.Domain.Services.PushNotifications;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MiCash.Application.Jobs.PushNotifications
{
    /// <summary>
    /// Sends Pending deliveries to Expo in batches of 100. Each batch is first claimed (Sending, committed
    /// with optimistic concurrency) and only then sent, so a re-run, a Hangfire retry or a parallel
    /// execution never sends the same delivery twice (at most once; doubtful cases end Unconfirmed).
    /// </summary>
    public sealed class PushNotificationDispatchJob : IPushNotificationDispatchJob
    {
        public const int BATCH_SIZE = IExpoPushClient.MAX_MESSAGES_PER_REQUEST;
        public const int MAX_BATCHES_PER_RUN = 20;
        public const int MAX_ATTEMPTS = 3;

        private readonly IPushDeliveryUpdateOnlyRepository _deliveryRepository;
        private readonly IExpoPushClient _expoPushClient;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<PushNotificationDispatchJob> _logger;

        public PushNotificationDispatchJob (
            IPushDeliveryUpdateOnlyRepository deliveryRepository ,
            IExpoPushClient expoPushClient ,
            IUnitOfWork unitOfWork ,
            TimeProvider timeProvider ,
            ILogger<PushNotificationDispatchJob> logger )
        {
            _deliveryRepository = deliveryRepository;
            _expoPushClient = expoPushClient;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        /// <summary>Wait before retrying a temporary failure: 1, 2, 4... minutes.</summary>
        public static TimeSpan Backoff ( int attempts ) => TimeSpan.FromMinutes(Math.Pow(2 , Math.Max(0 , attempts - 1)));

        public async Task ExecuteAsync ( long? notificationId , CancellationToken cancellationToken )
        {
            for ( var batch = 0; batch < MAX_BATCHES_PER_RUN; batch++ ) {
                var now = Now();

                var due = await _deliveryRepository.GetDueAsync(now , notificationId , BATCH_SIZE , cancellationToken);

                if ( due.Count == 0 ) {
                    return;
                }

                var toSend = Claim(due , now);

                try {
                    await _unitOfWork.Commit(cancellationToken);
                } catch ( ConflictException ) {
                    // Another execution claimed (some of) these deliveries first; it will send them.
                    _logger.LogInformation("Push dispatch: lote já reservado por outra execução." );

                    return;
                }

                if ( toSend.Count == 0 ) {
                    continue;
                }

                var keepGoing = await Send(toSend , cancellationToken);

                await _unitOfWork.Commit(cancellationToken);

                if ( keepGoing == false ) {
                    return;
                }
            }
        }

        private List<PushNotificationDelivery> Claim ( IReadOnlyList<PushNotificationDelivery> due , DateTime now )
        {
            var toSend = new List<PushNotificationDelivery>(due.Count);

            foreach ( var delivery in due ) {
                delivery.UpdatedAt = now;

                if ( delivery.Device.IsActive == false ) {
                    // Device deactivated after the notification was queued: nothing to send.
                    delivery.Status = PushDeliveryStatus.Failed;
                    delivery.ErrorCode = "DeviceInactive";
                    continue;
                }

                delivery.Status = PushDeliveryStatus.Sending;
                delivery.ClaimedAt = now;
                delivery.Attempts++;
                toSend.Add(delivery);
            }

            return toSend;
        }

        /// <returns>false when Expo is unavailable (stop this run; the next one retries).</returns>
        private async Task<bool> Send ( List<PushNotificationDelivery> deliveries , CancellationToken cancellationToken )
        {
            var messages = deliveries
                .Select(d => new ExpoPushMessage(d.Device.ExpoPushToken , d.Notification.Title , d.Notification.Body , ReadData(d.Notification)))
                .ToList();

            IReadOnlyList<ExpoPushTicket> tickets;

            try {
                tickets = await _expoPushClient.SendAsync(messages , cancellationToken);
            } catch ( PushServiceException ex ) {
                var now = Now();

                foreach ( var delivery in deliveries ) {
                    switch ( ex.Kind ) {
                        case PushFailureKind.Retryable:
                            RetryLater(delivery , "ExpoUnavailable" , ex.Message , now);
                            break;
                        case PushFailureKind.Uncertain:
                            Finish(delivery , PushDeliveryStatus.Unconfirmed , "SendUncertain" , ex.Message , now);
                            break;
                        default:
                            Finish(delivery , PushDeliveryStatus.Failed , "ExpoRejected" , ex.Message , now);
                            break;
                    }
                }

                _logger.LogWarning(
                    "Push dispatch: falha {Kind} ao enviar {Count} entrega(s) ao Expo." , ex.Kind , deliveries.Count);

                return ex.Kind != PushFailureKind.Retryable;
            }

            ApplyTickets(deliveries , tickets , Now());

            return true;
        }

        private void ApplyTickets ( List<PushNotificationDelivery> deliveries , IReadOnlyList<ExpoPushTicket> tickets , DateTime now )
        {
            for ( var i = 0; i < deliveries.Count; i++ ) {
                var delivery = deliveries[i];
                var ticket = tickets[i];

                if ( ticket.IsOk ) {
                    delivery.Status = PushDeliveryStatus.SentToExpo;
                    delivery.ExpoTicketId = ticket.Id;
                    delivery.SentAt = now;
                    delivery.ErrorCode = null;
                    delivery.ErrorMessage = null;
                    delivery.UpdatedAt = now;
                    delivery.Device.LastUsedAt = now;
                    continue;
                }

                if ( ticket.ErrorCode == ExpoPushErrors.MESSAGE_RATE_EXCEEDED ) {
                    RetryLater(delivery , ticket.ErrorCode , ticket.ErrorMessage , now);
                    continue;
                }

                Finish(delivery , PushDeliveryStatus.Failed , ticket.ErrorCode , ticket.ErrorMessage , now);

                if ( ticket.ErrorCode == ExpoPushErrors.DEVICE_NOT_REGISTERED ) {
                    PushDeviceDeactivation.Deactivate(delivery.Device , now , _logger);
                } else {
                    _logger.LogWarning(
                        "Push dispatch: ticket com erro {ErrorCode} na entrega {DeliveryId} (aparelho {TokenSuffix})." ,
                        ticket.ErrorCode , delivery.Id , PushTokenMask.Suffix(delivery.Device.ExpoPushToken));
                }
            }
        }

        private static void RetryLater ( PushNotificationDelivery delivery , string? code , string? message , DateTime now )
        {
            if ( delivery.Attempts >= MAX_ATTEMPTS ) {
                Finish(delivery , PushDeliveryStatus.Failed , code , message , now);
                return;
            }

            delivery.Status = PushDeliveryStatus.Pending;
            delivery.ClaimedAt = null;
            delivery.NextAttemptAt = now + Backoff(delivery.Attempts);
            delivery.ErrorCode = code;
            delivery.ErrorMessage = PushDeliveryText.Truncate(message);
            delivery.UpdatedAt = now;
        }

        private static void Finish ( PushNotificationDelivery delivery , PushDeliveryStatus status , string? code , string? message , DateTime now )
        {
            delivery.Status = status;
            delivery.ErrorCode = PushDeliveryText.TruncateCode(code);
            delivery.ErrorMessage = PushDeliveryText.Truncate(message);
            delivery.UpdatedAt = now;
        }

        /// <summary>
        /// Stored data (type, url) plus "notificationId", so the app can mark as read the notification
        /// the user tapped (the id only exists after the notification is saved, hence added here).
        /// </summary>
        private static IReadOnlyDictionary<string , string> ReadData ( PushNotification notification )
        {
            Dictionary<string , string> data;

            try {
                data = JsonSerializer.Deserialize<Dictionary<string , string>>(notification.Data) ?? [];
            } catch ( JsonException ) {
                data = [];
            }

            data[NOTIFICATION_ID_KEY] = notification.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);

            return data;
        }

        public const string NOTIFICATION_ID_KEY = "notificationId";

        private DateTime Now () => _timeProvider.GetUtcNow().UtcDateTime;
    }

    internal static class PushDeviceDeactivation
    {
        public static void Deactivate ( PushDevice device , DateTime now , ILogger logger )
        {
            if ( device.IsActive == false ) {
                return;
            }

            device.IsActive = false;
            device.UpdatedAt = now;

            logger.LogInformation(
                "Aparelho {DeviceId} ({TokenSuffix}) desativado: Expo informou DeviceNotRegistered." ,
                device.Id , PushTokenMask.Suffix(device.ExpoPushToken));
        }
    }

    internal static class PushDeliveryText
    {
        public const int MESSAGE_MAX_LENGTH = 500;
        public const int CODE_MAX_LENGTH = 100;

        public static string? Truncate ( string? text )
        {
            text = PushTokenMask.Sanitize(text);

            return text is { Length: > MESSAGE_MAX_LENGTH } ? text[..MESSAGE_MAX_LENGTH] : text;
        }

        public static string? TruncateCode ( string? code ) =>
            code is { Length: > CODE_MAX_LENGTH } ? code[..CODE_MAX_LENGTH] : code;
    }
}
