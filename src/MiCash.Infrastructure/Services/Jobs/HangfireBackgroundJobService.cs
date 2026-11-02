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

        public string EnqueuePushNotificationDispatch ( long notificationId )
        {
            return BackgroundJob.Enqueue<IPushNotificationDispatchJob>(
                x => x.ExecuteAsync(notificationId , CancellationToken.None));
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

        public bool Delete ( string jobId )
        {
            return BackgroundJob.Delete(jobId);
        }
    }
}
