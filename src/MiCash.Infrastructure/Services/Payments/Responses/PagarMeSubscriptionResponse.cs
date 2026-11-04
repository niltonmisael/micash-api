using System.Text.Json.Serialization;

namespace MiCash.Infrastructure.Services.Payments.Responses
{
    /// <summary>Subset of GET /subscriptions/{id} (Pagar.me Core v5) used by MiCash.</summary>
    public class PagarMeSubscriptionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("next_billing_at")]
        public DateTimeOffset? NextBillingAt { get; set; }

        [JsonPropertyName("canceled_at")]
        public DateTimeOffset? CanceledAt { get; set; }
    }
}
