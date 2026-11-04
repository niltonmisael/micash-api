using MiCash.Domain.Entities;
using MiCash.Domain.Enums;

namespace MiCash.Domain.Services.Plans
{
    /// <summary>
    /// The single rule of "is this subscription Premium right now", used by the plan limits, the
    /// [RequireActiveSubscription] filter and the subscription screen, whatever the client (web, Android, iOS).
    /// - Active: Premium (a canceled one stays Active until the paid period ends).
    /// - PaymentFailed after a renewal: Premium during the grace period while Pagar.me retries the charge.
    /// - Anything else (Pending, PaymentFailed on the first charge, Cancelled, Expired): Free.
    /// </summary>
    public static class SubscriptionAccess
    {
        public static readonly TimeSpan RenewalGracePeriod = TimeSpan.FromDays(3);

        public static bool HasPremium ( Subscription? subscription , DateTime now ) =>
            subscription is not null && (
                subscription.Status == SubscriptionStatus.Active ||
                InGracePeriod(subscription , now));

        public static bool InGracePeriod ( Subscription subscription , DateTime now ) =>
            subscription.Status == SubscriptionStatus.PaymentFailed &&
            subscription.PaymentGraceUntil is { } graceUntil &&
            graceUntil > now;
    }
}
