namespace MiCash.Communication.Responses.Subscriptions
{
    public class ResponseCurrentSubscriptionJson
    {
        public long? PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public int Status { get; set; }

        public string StatusDescription { get; set; } = string.Empty;

        public bool CancelAtPeriodEnd { get; set; }

        public DateTime? NextBillingDate { get; set; }

        public DateTime? EndsAt { get; set; }

        public DateTime? CancellationRequestedAt { get; set; }

        public DateTime? CancellationScheduledFor { get; set; }

        public decimal Price { get; set; }
    }
}
