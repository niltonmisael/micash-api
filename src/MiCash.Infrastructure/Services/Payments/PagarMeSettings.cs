namespace MiCash.Infrastructure.Services.Payments
{
    public class PagarMeSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Where Pagar.me sends the buyer after paying (payment link flow_settings.success_url), e.g. the web
        /// subscription page. Empty: Pagar.me keeps its own confirmation page.
        /// </summary>
        public string CheckoutSuccessUrl { get; set; } = string.Empty;
    }
}
