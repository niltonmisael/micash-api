using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Services.Jobs;

namespace MiCash.Application.Services.Subscriptions
{
    /// <summary>
    /// Cancellation policy: canceling stops the next charges only. The user keeps Premium until the end of the
    /// period already paid (NextBillingDate) and then goes back to Free.
    ///
    /// Lifecycle of a cancellation requested by the user:
    /// 1. Request: CancelAtPeriodEnd = true, CancellationScheduledFor = end of the paid period, job scheduled to
    ///    cancel at Pagar.me a few hours before it (still reversible by reactivating).
    /// 2. Canceled at Pagar.me: Status stays Active (access kept), EndsAt = end of the paid period (marks
    ///    "charges stopped", no longer reversible), expiration job scheduled for EndsAt.
    /// 3. Expiration (EndsAt reached): Status = Cancelled → Free.
    /// </summary>
    public static class SubscriptionCancellationRules
    {
        /// <summary>The subscription was canceled at the gateway: keeps the paid period, or closes it right away.</summary>
        public static void ApplyGatewayCancellation (
            Subscription subscription ,
            DateTime? gatewayCanceledAt ,
            DateTime now ,
            IBackgroundJobService jobs )
        {
            if ( subscription.Status == SubscriptionStatus.Cancelled ) {
                return;
            }

            var paidUntil = subscription.NextBillingDate;

            // Only a paid (Active) period is kept; Pending/PaymentFailed never got access, so they end now.
            if ( subscription.Status == SubscriptionStatus.Active && paidUntil > now ) {
                KeepAccessUntil(subscription , paidUntil.Value , jobs);
                return;
            }

            DeleteScheduledJob(subscription , jobs);
            Close(subscription , gatewayCanceledAt ?? now);
        }

        /// <summary>Charges stopped; access kept until <paramref name="accessEndsAt"/>, when it expires.</summary>
        public static void KeepAccessUntil ( Subscription subscription , DateTime accessEndsAt , IBackgroundJobService jobs )
        {
            // Already in this state (e.g. subscription.canceled right after our own cancellation job).
            if ( subscription.EndsAt == accessEndsAt && !string.IsNullOrWhiteSpace(subscription.CancellationJobId) ) {
                return;
            }

            DeleteScheduledJob(subscription , jobs);

            subscription.EndsAt = accessEndsAt;
            subscription.CancelAtPeriodEnd = true;
            subscription.CancellationScheduledFor = accessEndsAt;
            subscription.CancellationJobId = jobs.ScheduleSubscriptionExpiration(subscription.Id , accessEndsAt);
        }

        /// <summary>Ends the access: the user is Free from now on.</summary>
        public static void Close ( Subscription subscription , DateTime endedAt )
        {
            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.EndsAt = endedAt;
            subscription.CancelAtPeriodEnd = false;
            subscription.CancellationJobId = null;
        }

        /// <summary>Charges already stopped at the gateway: the cancellation can no longer be undone.</summary>
        public static bool ChargesStopped ( Subscription subscription ) =>
            subscription.Status == SubscriptionStatus.Active && subscription.EndsAt is not null;

        private static void DeleteScheduledJob ( Subscription subscription , IBackgroundJobService jobs )
        {
            if ( !string.IsNullOrWhiteSpace(subscription.CancellationJobId) ) {
                jobs.Delete(subscription.CancellationJobId);
                subscription.CancellationJobId = null;
            }
        }
    }
}
