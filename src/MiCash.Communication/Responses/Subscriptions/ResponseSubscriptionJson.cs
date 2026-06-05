namespace MiCash.Communication.Responses.Subscriptions
{
    public class ResponseSubscriptionJson
    {
        public long Id { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime? NextBillingDate { get; set; }
    }
}
