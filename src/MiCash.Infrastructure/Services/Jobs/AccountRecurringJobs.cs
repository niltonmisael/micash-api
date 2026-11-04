using Hangfire;
using MiCash.Domain.Services.Jobs;

namespace MiCash.Infrastructure.Services.Jobs
{
    /// <summary>Recurring jobs of the accounts: sign-up cleanup and the monthly summary.</summary>
    public static class AccountRecurringJobs
    {
        public const string UNVERIFIED_CLEANUP_JOB_ID = "accounts-unverified-cleanup";

        public const string MONTHLY_SUMMARY_JOB_ID = "accounts-monthly-summary";

        public static void Register ( IRecurringJobManager recurringJobs )
        {
            // 07:00 UTC = 04:00 in Brasília: off-peak, after the subscriptions reconciliation.
            recurringJobs.AddOrUpdate<IUnverifiedAccountCleanupJob>(
                UNVERIFIED_CLEANUP_JOB_ID ,
                job => job.ExecuteAsync(CancellationToken.None) ,
                "0 7 * * *");

            // Day 1 at 12:00 UTC = 09:00 in Brasília: the summary of the month that just ended.
            recurringJobs.AddOrUpdate<IMonthlySummaryDispatchJob>(
                MONTHLY_SUMMARY_JOB_ID ,
                job => job.ExecuteAsync(CancellationToken.None) ,
                "0 12 1 * *");
        }
    }
}
