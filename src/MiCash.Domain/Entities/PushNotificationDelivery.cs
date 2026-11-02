using MiCash.Domain.Enums;

namespace MiCash.Domain.Entities
{
    /// <summary>One notification sent to one device: ticket, receipt and error of that send.</summary>
    public class PushNotificationDelivery
    {
        public long Id { get; set; }

        public long NotificationId { get; set; }

        public PushNotification Notification { get; set; } = default!;

        public long DeviceId { get; set; }

        public PushDevice Device { get; set; } = default!;

        public PushDeliveryStatus Status { get; set; } = PushDeliveryStatus.Pending;

        /// <summary>Send attempts so far (temporary failures are retried up to a limit).</summary>
        public int Attempts { get; set; }

        /// <summary>Not sent before this moment (backoff after a temporary failure).</summary>
        public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;

        /// <summary>When a job moved it to <see cref="PushDeliveryStatus.Sending"/>.</summary>
        public DateTime? ClaimedAt { get; set; }

        public string? ExpoTicketId { get; set; }

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime? SentAt { get; set; }

        public DateTime? ReceiptCheckedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
