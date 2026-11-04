using Hangfire;
using MiCash.Domain.Services.Jobs;

namespace MiCash.Infrastructure.Services.Jobs
{
    /// <summary>Recurring jobs of the accounts (sign-up) flow.</summary>
    public static class AccountRecurringJobs
    {
        public const string UNVERIFIED_CLEANUP_JOB_ID = "accounts-unverified-cleanup";

        public static void Register ( IRecurringJobManager recurringJobs )
        {
            // 07:00 UTC = 04:00 in Brasília: off-peak, after the subscriptions reconciliation.
            recurringJobs.AddOrUpdate<IUnverifiedAccountCleanupJob>(
                UNVERIFIED_CLEANUP_JOB_ID ,
                job => job.ExecuteAsync(CancellationToken.None) ,
                "0 7 * * *");
        }
    }
}
