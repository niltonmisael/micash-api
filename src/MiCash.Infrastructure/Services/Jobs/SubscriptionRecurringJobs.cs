using Hangfire;
using MiCash.Domain.Services.Jobs;

namespace MiCash.Infrastructure.Services.Jobs
{
    /// <summary>Recurring jobs of the subscriptions/payments flow.</summary>
    public static class SubscriptionRecurringJobs
    {
        public const string RECONCILIATION_JOB_ID = "subscriptions-reconciliation";

        public static void Register ( IRecurringJobManager recurringJobs )
        {
            // 06:00 UTC = 03:00 in Brasília: off-peak, after the night's renewals were attempted.
            recurringJobs.AddOrUpdate<ISubscriptionReconciliationJob>(
                RECONCILIATION_JOB_ID ,
                job => job.ExecuteAsync(CancellationToken.None) ,
                "0 6 * * *");
        }
    }
}
