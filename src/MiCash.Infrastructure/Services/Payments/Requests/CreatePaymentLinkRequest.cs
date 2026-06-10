using System.Text.Json.Serialization;

namespace MiCash.Infrastructure.Services.Payments.Requests
{
    public class CreatePaymentLinkRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "subscription";

        [JsonPropertyName("order_code")]
        public string OrderCode { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        public Dictionary<string , string> Metadata { get; set; } = [];

        [JsonPropertyName("is_building")]
        public bool IsBuilding { get; set; } = false;

        [JsonPropertyName("payment_settings")]
        public PaymentSettings PaymentSettings { get; set; } = default!;

        [JsonPropertyName("cart_settings")]
        public PaymentLinkCartSettings CartSettings { get; set; } = default!;
    }

    public class PaymentSettings
    {
        [JsonPropertyName("accepted_payment_methods")]
        public List<string> AcceptedPaymentMethods { get; set; } = [];

        [JsonPropertyName("credit_card_settings")]
        public CreditCardSettings CreditCardSettings { get; set; } = default!;
    }

    public class CreditCardSettings
    {
        [JsonPropertyName("operation_type")]
        public string OperationType { get; set; } = "auth_and_capture";
    }

    public class PaymentLinkCartSettings
    {
        [JsonPropertyName("recurrences")]
        public List<PaymentLinkRecurrence> Recurrences { get; set; } = [];
    }

    public class PaymentLinkRecurrence
    {
        [JsonPropertyName("plan_id")]
        public string PlanId { get; set; } = string.Empty;
    }

}
