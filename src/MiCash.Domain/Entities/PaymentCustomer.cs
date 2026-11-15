namespace MiCash.Domain.Entities
{
    /// <summary>
    /// A payer (gateway customer) used by the account holder <see cref="UserId"/>. The payer may be someone else
    /// (mother, husband) and needs no MiCash account. One row per (user, provider, external id): a user has as many
    /// payers as people who paid for them, and the same gateway customer may pay for several users (one row each).
    /// Never deleted on cancellation: subscriptions keep pointing to the payer used when they were contracted.
    /// The customer itself is created by the hosted checkout, never by MiCash.
    /// </summary>
    public class PaymentCustomer
    {
        public const string PagarmeProvider = "pagarme";

        public long Id { get; set; }
        public long UserId { get; set; }
        public User User { get; set; } = default!;
        public string Provider { get; set; } = PagarmeProvider;
        public string ExternalCustomerId { get; set; } = string.Empty;

        /// <summary>Snapshot from the latest webhook (display only). No document: a third party's CPF is not stored (LGPD).</summary>
        public string? Name { get; set; }
        public string? Email { get; set; }

        public DateTime FirstUsedAt { get; set; }
        public DateTime LastUsedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Subscription> Subscriptions { get; set; } = [];
    }
}
