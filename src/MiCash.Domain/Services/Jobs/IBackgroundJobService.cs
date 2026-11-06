namespace MiCash.Domain.Services.Jobs
{
    public interface IBackgroundJobService
    {
        /// <summary>Stops the charges at the gateway shortly before the next billing date (user keeps access).</summary>
        string ScheduleSubscriptionCancellation ( long subscriptionId , DateTime executeAt );

        /// <summary>Ends the access (back to Free) of a canceled subscription when its paid period is over.</summary>
        string ScheduleSubscriptionExpiration ( long subscriptionId , DateTime executeAt );

        /// <summary>Sends a just-created notification right away (the recurring dispatch is the fallback).</summary>
        string EnqueuePushNotificationDispatch ( long notificationId );

        string EnqueueWelcomeEmail ( long userId );

        string EnqueuePasswordResetEmail ( long passwordResetRequestId );

        string EnqueueEmailVerificationEmail ( long emailVerificationRequestId );

        string EnqueueEmailChangedNotice ( long emailVerificationRequestId , string previousEmail );

        /// <summary>Deletion notice; the account is already gone, so the address and name go with the job.</summary>
        string EnqueueAccountDeletedEmail ( long userId , string email , string name );

        string EnqueueMonthlySummary ( long userId , int year , int month );

        bool Delete ( string jobId );
    }
}
