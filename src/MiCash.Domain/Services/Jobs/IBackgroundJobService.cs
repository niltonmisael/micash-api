namespace MiCash.Domain.Services.Jobs
{
    public interface IBackgroundJobService
    {
        string ScheduleSubscriptionCancellation ( long subscriptionId , DateTime executeAt );

        /// <summary>Sends a just-created notification right away (the recurring dispatch is the fallback).</summary>
        string EnqueuePushNotificationDispatch ( long notificationId );

        string EnqueueWelcomeEmail ( long userId );

        string EnqueuePasswordResetEmail ( long passwordResetRequestId );

        bool Delete ( string jobId );
    }
}
