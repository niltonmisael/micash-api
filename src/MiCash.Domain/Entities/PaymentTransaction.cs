using MiCash.Domain.Enums;

namespace MiCash.Domain.Entities
{
    /// <summary>
    /// One gateway charge of a subscription (first payment, renewals, retries), upserted by
    /// <see cref="ExternalChargeId"/>. Belongs to the subscription the charge was billed for, so the charges of an
    /// old contract never mix with a new one. The payer is the subscription's <see cref="PaymentCustomer"/>.
    /// </summary>
    public class PaymentTransaction
    {
        public long Id { get; set; }
        public long SubscriptionId { get; set; }
        public Subscription Subscription { get; set; } = default!;
        public string ExternalChargeId { get; set; } = string.Empty;

        /// <summary>In cents, as sent by the gateway. Null when the event did not carry it.</summary>
        public long? AmountCents { get; set; }
        public PaymentTransactionStatus Status { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? FailedAt { get; set; }
        public DateTime? RefundedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
