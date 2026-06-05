namespace MiCash.Communication.Responses.Subscriptions
{
    public class ResponseCurrentSubscriptionJson
    {
        public long Id { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime? NextBillingDate { get; set; }
    }
}
