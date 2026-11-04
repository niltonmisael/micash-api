using MiCash.Domain.Services.Jobs;
using System.Collections.Concurrent;

namespace CommonTestUtilities.Jobs
{
    /// <summary>Records what would be handed to Hangfire (tests have no Hangfire storage).</summary>
    public sealed class FakeBackgroundJobService : IBackgroundJobService
    {
        public ConcurrentQueue<long> EnqueuedPushNotifications { get; } = new();

        public bool ThrowOnEnqueue { get; set; }

        public ConcurrentQueue<(long SubscriptionId, DateTime ExecuteAt)> ScheduledExpirations { get; } = new();

        public string ScheduleSubscriptionCancellation ( long subscriptionId , DateTime executeAt ) => $"sub-{subscriptionId}";

        public string ScheduleSubscriptionExpiration ( long subscriptionId , DateTime executeAt )
        {
            ScheduledExpirations.Enqueue((subscriptionId, executeAt));
            return $"expire-{subscriptionId}";
        }

        public string EnqueuePushNotificationDispatch ( long notificationId )
        {
            if ( ThrowOnEnqueue ) {
                throw new InvalidOperationException("Hangfire storage unavailable.");
            }

            EnqueuedPushNotifications.Enqueue(notificationId);

            return $"push-{notificationId}";
        }

        public ConcurrentQueue<long> EnqueuedWelcomeEmails { get; } = new();

        public ConcurrentQueue<long> EnqueuedPasswordResetEmails { get; } = new();

        public string EnqueueWelcomeEmail ( long userId )
        {
            if ( ThrowOnEnqueue ) {
                throw new InvalidOperationException("Hangfire storage unavailable.");
            }

            EnqueuedWelcomeEmails.Enqueue(userId);

            return $"welcome-{userId}";
        }

        public string EnqueuePasswordResetEmail ( long passwordResetRequestId )
        {
            if ( ThrowOnEnqueue ) {
                throw new InvalidOperationException("Hangfire storage unavailable.");
            }

            EnqueuedPasswordResetEmails.Enqueue(passwordResetRequestId);

            return $"password-reset-{passwordResetRequestId}";
        }

        public ConcurrentQueue<long> EnqueuedEmailVerificationEmails { get; } = new();

        public string EnqueueEmailVerificationEmail ( long emailVerificationRequestId )
        {
            if ( ThrowOnEnqueue ) {
                throw new InvalidOperationException("Hangfire storage unavailable.");
            }

            EnqueuedEmailVerificationEmails.Enqueue(emailVerificationRequestId);

            return $"email-verification-{emailVerificationRequestId}";
        }

        public ConcurrentQueue<(long RequestId, string PreviousEmail)> EnqueuedEmailChangedNotices { get; } = new();

        public string EnqueueEmailChangedNotice ( long emailVerificationRequestId , string previousEmail )
        {
            if ( ThrowOnEnqueue ) {
                throw new InvalidOperationException("Hangfire storage unavailable.");
            }

            EnqueuedEmailChangedNotices.Enqueue((emailVerificationRequestId, previousEmail));

            return $"email-changed-{emailVerificationRequestId}";
        }

        public bool Delete ( string jobId ) => true;
    }
}
