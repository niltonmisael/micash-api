using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.PushNotifications
{
    public interface IPushDeviceReadOnlyRepository
    {
        Task<IReadOnlyList<PushDevice>> GetAllByUserAsync ( long userId , CancellationToken cancellationToken );

        Task<IReadOnlyList<PushDevice>> GetActiveByUserAsync ( long userId , CancellationToken cancellationToken );
    }

    public interface IPushDeviceWriteOnlyRepository
    {
        Task AddAsync ( PushDevice device , CancellationToken cancellationToken );
    }

    public interface IPushDeviceUpdateOnlyRepository
    {
        /// <summary>Tracked, regardless of the owner (a token is unique across users).</summary>
        Task<PushDevice?> GetByTokenAsync ( string expoPushToken , CancellationToken cancellationToken );

        /// <summary>Tracked active devices of the user reporting this DeviceId (same install, older tokens).</summary>
        Task<IReadOnlyList<PushDevice>> GetActiveByDeviceIdAsync ( long userId , string deviceId , CancellationToken cancellationToken );
    }

    public interface IPushNotificationReadOnlyRepository
    {
        Task<bool> ExistsAsync ( long userId , string idempotencyKey , CancellationToken cancellationToken );
    }

    /// <summary>Notification center (the user's notifications in the app).</summary>
    public interface INotificationCenterReadOnlyRepository
    {
        /// <summary>Newest first; <paramref name="before"/> = only ids lower than it (cursor).</summary>
        Task<IReadOnlyList<PushNotification>> GetPageAsync ( long userId , long? before , int take , CancellationToken cancellationToken );

        Task<int> CountUnreadAsync ( long userId , CancellationToken cancellationToken );
    }

    /// <summary>Tracked entities; saved by <see cref="IUnitOfWork.Commit"/>.</summary>
    public interface INotificationCenterUpdateOnlyRepository
    {
        /// <summary>The notification only when it belongs to the user.</summary>
        Task<PushNotification?> GetByIdAsync ( long userId , long id , CancellationToken cancellationToken );

        Task<IReadOnlyList<PushNotification>> GetUnreadAsync ( long userId , CancellationToken cancellationToken );
    }

    public interface IPushNotificationWriteOnlyRepository
    {
        /// <summary>Adds the notification together with its deliveries.</summary>
        Task AddAsync ( PushNotification notification , CancellationToken cancellationToken );
    }

    /// <summary>Queries used by the jobs. Everything returned is tracked, with Notification and Device loaded.</summary>
    public interface IPushDeliveryUpdateOnlyRepository
    {
        /// <summary>Pending deliveries due at <paramref name="now"/>, oldest first.</summary>
        Task<IReadOnlyList<PushNotificationDelivery>> GetDueAsync (
            DateTime now , long? notificationId , int take , CancellationToken cancellationToken );

        /// <summary>Deliveries with a ticket sent before <paramref name="sentBefore"/>, least recently checked first.</summary>
        Task<IReadOnlyList<PushNotificationDelivery>> GetAwaitingReceiptAsync (
            DateTime sentBefore , int take , CancellationToken cancellationToken );

        /// <summary>Deliveries left in Sending (job interrupted) since before <paramref name="claimedBefore"/>.</summary>
        Task<IReadOnlyList<PushNotificationDelivery>> GetStuckSendingAsync (
            DateTime claimedBefore , int take , CancellationToken cancellationToken );
    }
}
