namespace MiCash.Domain.Services.Payments.Models
{
    public class CreateCheckoutResult
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public string ExternalSubscriptionId { get; set; } = string.Empty;
    }
}
