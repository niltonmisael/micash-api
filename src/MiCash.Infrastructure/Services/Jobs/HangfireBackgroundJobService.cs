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

        public bool Delete ( string jobId )
        {
            return BackgroundJob.Delete(jobId);
        }
    }
}
