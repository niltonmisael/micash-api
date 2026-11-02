namespace MiCash.Domain.Services.Jobs
{
    public interface IPushNotificationDispatchJob
    {
        /// <summary>
        /// Sends due Pending deliveries (of one notification, or all when null). Idempotent: a delivery is
        /// claimed (Sending) before the request to Expo and is never sent again from that state.
        /// </summary>
        Task ExecuteAsync ( long? notificationId , CancellationToken cancellationToken );
    }

    public interface IPushReceiptCheckJob
    {
        /// <summary>Reads Expo receipts of sent tickets, updates statuses and deactivates unregistered devices.</summary>
        Task ExecuteAsync ( CancellationToken cancellationToken );
    }
}
