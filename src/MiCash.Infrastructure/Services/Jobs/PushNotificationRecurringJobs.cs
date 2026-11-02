using Hangfire;
using MiCash.Domain.Services.Jobs;

namespace MiCash.Infrastructure.Services.Jobs
{
    /// <summary>Recurring jobs of the push infrastructure, on the existing Hangfire server/storage.</summary>
    public static class PushNotificationRecurringJobs
    {
        public const string DISPATCH_JOB_ID = "push-notifications-dispatch";
        public const string RECEIPTS_JOB_ID = "push-notifications-receipts";

        public static void Register ( IRecurringJobManager recurringJobs )
        {
            // Fallback for the immediate enqueue and for retries after temporary failures.
            recurringJobs.AddOrUpdate<IPushNotificationDispatchJob>(
                DISPATCH_JOB_ID ,
                job => job.ExecuteAsync(null , CancellationToken.None) ,
                Cron.Minutely());

            // Expo recommends reading receipts ~15 min after sending; the job only picks tickets that old.
            recurringJobs.AddOrUpdate<IPushReceiptCheckJob>(
                RECEIPTS_JOB_ID ,
                job => job.ExecuteAsync(CancellationToken.None) ,
                "*/5 * * * *");
        }
    }
}
