namespace MiCash.Domain.Services.PushNotifications
{
    /// <summary>
    /// A notification to a user, independent of the business rule that produced it.
    /// </summary>
    /// <param name="Type">Short identifier of the origin (e.g. "test").</param>
    /// <param name="Route">Internal app route opened on tap (see <see cref="PushRoutes"/>); null = just opens the app.</param>
    /// <param name="IdempotencyKey">Same key for the same user = the notification is created only once.</param>
    public sealed record PushMessage (
        string Type ,
        string Title ,
        string Body ,
        string? Route = null ,
        string? IdempotencyKey = null );

    public interface IPushNotificationService
    {
        /// <summary>
        /// Records the notification (it always appears in the notification center) with one delivery per active
        /// device of the user and schedules the push. Without active devices it is recorded with no delivery
        /// (Devices = 0, nothing sent). Returns null only when the idempotency key was already used.
        /// Commits its own unit of work.
        /// </summary>
        Task<PushEnqueueResult?> EnqueueAsync ( long userId , PushMessage message , CancellationToken cancellationToken );
    }

    public sealed record PushEnqueueResult ( long NotificationId , int Devices );
}
