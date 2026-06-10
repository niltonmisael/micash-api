namespace MiCash.Domain.Entities
{
    public class ProcessedWebhook
    {
        public long Id { get; set; }

        public string WebhookId { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;

        public string Payload { get; set; } = string.Empty;

        public bool Success { get; set; }

        public string? Error { get; set; }

        public DateTime ProcessedAt { get; set; }
    }
}
