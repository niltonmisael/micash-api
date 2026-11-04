namespace MiCash.Communication.Responses.Subscriptions
{
    /// <summary>
    /// The user's subscription as the server sees it, for any client (web, Android, iOS). Read-only: the
    /// subscription only changes through Pagar.me webhooks and the server's own jobs. Detailed permissions
    /// are in GET api/subscription/limits.
    /// </summary>
    public class ResponseCurrentSubscriptionJson
    {
        public long? PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        /// <summary>SubscriptionStatus as a number. Kept for compatibility; prefer <see cref="StatusDescription"/> and <see cref="IsPremium"/> (Free is 0, the same number as Pending).</summary>
        public int Status { get; set; }

        /// <summary>"Free", "Pending", "Active", "PaymentFailed"…</summary>
        public string StatusDescription { get; set; } = string.Empty;

        /// <summary>Whether the user has the Premium benefits now (Active, or refused renewal within the grace period).</summary>
        public bool IsPremium { get; set; }

        public DateTime? StartsAt { get; set; }

        public bool CancelAtPeriodEnd { get; set; }

        /// <summary>The subscription renews by itself at <see cref="NextBillingDate"/> (Active and not canceled).</summary>
        public bool AutoRenew { get; set; }

        public DateTime? NextBillingDate { get; set; }

        /// <summary>
        /// When Premium ends if nothing changes: end of the paid period of a canceled subscription, or end of the
        /// grace period of a refused renewal. Null while it renews normally (or for Free).
        /// </summary>
        public DateTime? AccessEndsAt { get; set; }

        /// <summary>Refused renewal: Premium kept until this moment while the charge is retried.</summary>
        public DateTime? PaymentGraceUntil { get; set; }

        public DateTime? EndsAt { get; set; }

        public DateTime? CancellationRequestedAt { get; set; }

        public DateTime? CancellationScheduledFor { get; set; }

        public decimal Price { get; set; }

        /// <summary>"Monthly" or "Yearly"; null for Free.</summary>
        public string? BillingPeriod { get; set; }
    }
}
