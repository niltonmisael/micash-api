using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.PushNotifications;
using MiCash.Domain.Services.Jobs;
using MiCash.Domain.Services.PushNotifications;
using Microsoft.Extensions.Logging;

namespace MiCash.Application.Jobs.PushNotifications
{
    /// <summary>
    /// A ticket only means Expo accepted the message. This job reads the receipts (available ~15 min
    /// after sending, kept for 24 h) to know whether FCM/APNs got it, and deactivates devices Expo
    /// reports as DeviceNotRegistered. Temporary failures never deactivate anything.
    /// </summary>
    public sealed class PushReceiptCheckJob : IPushReceiptCheckJob
    {
        public static readonly TimeSpan ReceiptDelay = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan ReceiptExpiration = TimeSpan.FromHours(24);
        public static readonly TimeSpan StuckSendingAfter = TimeSpan.FromMinutes(10);
        public const int STUCK_BATCH_SIZE = 500;

        private readonly IPushDeliveryUpdateOnlyRepository _deliveryRepository;
        private readonly IExpoPushClient _expoPushClient;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<PushReceiptCheckJob> _logger;

        public PushReceiptCheckJob (
            IPushDeliveryUpdateOnlyRepository deliveryRepository ,
            IExpoPushClient expoPushClient ,
            IUnitOfWork unitOfWork ,
            TimeProvider timeProvider ,
            ILogger<PushReceiptCheckJob> logger )
        {
            _deliveryRepository = deliveryRepository;
            _expoPushClient = expoPushClient;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task ExecuteAsync ( CancellationToken cancellationToken )
        {
            await ReleaseStuckSending(cancellationToken);

            await CheckReceipts(cancellationToken);
        }

        /// <summary>
        /// A job interrupted between claiming and saving the ticket: Expo may or may not have sent it,
        /// so it is NOT resent (no duplicates) and ends Unconfirmed.
        /// </summary>
        private async Task ReleaseStuckSending ( CancellationToken cancellationToken )
        {
            var now = Now();

            var stuck = await _deliveryRepository.GetStuckSendingAsync(now - StuckSendingAfter , STUCK_BATCH_SIZE , cancellationToken);

            if ( stuck.Count == 0 ) {
                return;
            }

            foreach ( var delivery in stuck ) {
                delivery.Status = PushDeliveryStatus.Unconfirmed;
                delivery.ErrorCode = "SendInterrupted";
                delivery.UpdatedAt = now;
            }

            await _unitOfWork.Commit(cancellationToken);

            _logger.LogWarning("Push receipts: {Count} entrega(s) presas em Sending marcadas como Unconfirmed." , stuck.Count);
        }

        private async Task CheckReceipts ( CancellationToken cancellationToken )
        {
            var now = Now();

            var awaiting = await _deliveryRepository.GetAwaitingReceiptAsync(
                now - ReceiptDelay , IExpoPushClient.MAX_RECEIPT_IDS_PER_REQUEST , cancellationToken);

            if ( awaiting.Count == 0 ) {
                return;
            }

            IReadOnlyDictionary<string , ExpoPushReceipt> receipts;

            try {
                receipts = await _expoPushClient.GetReceiptsAsync(
                    awaiting.Select(d => d.ExpoTicketId!).ToList() , cancellationToken);
            } catch ( PushServiceException ex ) {
                // Nothing changes: the same tickets are checked on the next run.
                _logger.LogWarning("Push receipts: consulta falhou ({Kind}); nova tentativa na próxima execução." , ex.Kind);

                return;
            }

            int delivered = 0, failed = 0, expired = 0;

            foreach ( var delivery in awaiting ) {
                delivery.ReceiptCheckedAt = now;
                delivery.UpdatedAt = now;

                if ( receipts.TryGetValue(delivery.ExpoTicketId! , out var receipt) ) {
                    if ( receipt.IsOk ) {
                        delivery.Status = PushDeliveryStatus.Delivered;
                        delivered++;
                        continue;
                    }

                    delivery.Status = PushDeliveryStatus.Failed;
                    delivery.ErrorCode = PushDeliveryText.TruncateCode(receipt.ErrorCode);
                    delivery.ErrorMessage = PushDeliveryText.Truncate(receipt.ErrorMessage);
                    failed++;

                    if ( receipt.ErrorCode == ExpoPushErrors.DEVICE_NOT_REGISTERED ) {
                        PushDeviceDeactivation.Deactivate(delivery.Device , now , _logger);
                    } else {
                        _logger.LogWarning(
                            "Push receipts: erro {ErrorCode} na entrega {DeliveryId} (aparelho {TokenSuffix})." ,
                            receipt.ErrorCode , delivery.Id , PushTokenMask.Suffix(delivery.Device.ExpoPushToken));
                    }

                    continue;
                }

                // No receipt yet. After 24 h Expo discards them: the outcome stays unknown.
                if ( delivery.SentAt <= now - ReceiptExpiration ) {
                    delivery.Status = PushDeliveryStatus.Unconfirmed;
                    delivery.ErrorCode = "ReceiptNotAvailable";
                    expired++;
                }
            }

            await _unitOfWork.Commit(cancellationToken);

            _logger.LogInformation(
                "Push receipts: {Checked} verificados, {Delivered} entregues, {Failed} com erro, {Expired} sem confirmação." ,
                awaiting.Count , delivered , failed , expired);
        }

        private DateTime Now () => _timeProvider.GetUtcNow().UtcDateTime;
    }
}
