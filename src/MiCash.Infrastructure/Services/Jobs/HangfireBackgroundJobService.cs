using Hangfire;
using MiCash.Domain.Services.Jobs;

namespace MiCash.Infrastructure.Services.Jobs
{
    public class HangfireBackgroundJobService : IBackgroundJobService
    {
        public string ScheduleSubscriptionCancellation (long subscriptionId , DateTime executeAt )
        {
            return BackgroundJob.Schedule<ISubscriptionCancellationJob>(
                x => x.ExecuteAsync( subscriptionId , CancellationToken.None) ,executeAt );
        }

        public string ScheduleSubscriptionExpiration ( long subscriptionId , DateTime executeAt )
        {
            return BackgroundJob.Schedule<ISubscriptionExpirationJob>(
                x => x.ExecuteAsync( subscriptionId , CancellationToken.None) ,executeAt );
        }

        public string EnqueuePushNotificationDispatch ( long notificationId )
        {
            return BackgroundJob.Enqueue<IPushNotificationDispatchJob>(
                x => x.ExecuteAsync(notificationId , CancellationToken.None));
        }

        public string EnqueueMonthlySummary ( long userId , int year , int month )
        {
            return BackgroundJob.Enqueue<IMonthlySummaryJob>(
                x => x.ExecuteAsync(userId , year , month , CancellationToken.None));
        }

        public string EnqueueWelcomeEmail ( long userId )
        {
            return BackgroundJob.Enqueue<IWelcomeEmailJob>(
                x => x.ExecuteAsync(userId , CancellationToken.None));
        }

        public string EnqueuePasswordResetEmail ( long passwordResetRequestId )
        {
            return BackgroundJob.Enqueue<IPasswordResetEmailJob>(
                x => x.ExecuteAsync(passwordResetRequestId , CancellationToken.None));
        }

        public string EnqueueEmailVerificationEmail ( long emailVerificationRequestId )
        {
            return BackgroundJob.Enqueue<IEmailVerificationEmailJob>(
                x => x.ExecuteAsync(emailVerificationRequestId , CancellationToken.None));
        }

        public string EnqueueEmailChangedNotice ( long emailVerificationRequestId , string previousEmail )
        {
            return BackgroundJob.Enqueue<IEmailChangedNoticeJob>(
                x => x.ExecuteAsync(emailVerificationRequestId , previousEmail , CancellationToken.None));
        }

        public string EnqueueAccountDeletedEmail ( long userId , string email , string name )
        {
            return BackgroundJob.Enqueue<IAccountDeletedEmailJob>(
                x => x.ExecuteAsync(userId , email , name , CancellationToken.None));
        }

        public bool Delete ( string jobId )
        {
            return BackgroundJob.Delete(jobId);
        }
    }
}
