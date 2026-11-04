namespace MiCash.Domain.Services.Payments.Models
{
    /// <summary>Snapshot of a subscription as the payment gateway sees it (source of truth for billing).</summary>
    public class GatewaySubscription
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>Raw gateway status (Pagar.me: active, canceled, future, failed).</summary>
        public string Status { get; set; } = string.Empty;

        public bool IsCanceled => string.Equals(Status , "canceled" , StringComparison.OrdinalIgnoreCase);

        /// <summary>UTC.</summary>
        public DateTime? NextBillingAt { get; set; }

        /// <summary>UTC.</summary>
        public DateTime? CanceledAt { get; set; }
    }
}
