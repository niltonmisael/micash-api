namespace MiCash.Communication.Responses.Subscriptions
{
    public class ResponseSubscriptionHistoryJson
    {
        public long SubscriptionId { get; set; }

        public long PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public int Status { get; set; }

        public string StatusDescription { get; set; } = string.Empty;

        public DateTime? StartsAt { get; set; }

        public DateTime? EndsAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
