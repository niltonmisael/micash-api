namespace MiCash.Domain.Services.Payments.Models
{
    public class CreateCheckoutResult
    {
        public string PaymentLinkId { get; set; } = string.Empty;

        public string CheckoutUrl { get; set; } = string.Empty;

        public string OrderCode { get; set; } = string.Empty;
    }
}
