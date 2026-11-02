namespace MiCash.Domain.Entities
{
    /// <summary>
    /// A notification of a user: it is both the in-app notification center entry (always recorded, read state in
    /// <see cref="ReadAt"/>) and the push sent to the user's active devices (one <see cref="Deliveries"/> row per
    /// device; none when the user had no active device).
    /// </summary>
    public class PushNotification
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public User User { get; set; } = default!;

        /// <summary>What produced it (e.g. "test"; later "bill-due", "goal-reached"...).</summary>
        public string Type { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        /// <summary>JSON object sent as Expo "data" ("url" = internal app route, when set).</summary>
        public string Data { get; set; } = "{}";

        /// <summary>Optional key that makes the same business event produce a single notification per user.</summary>
        public string? IdempotencyKey { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When the user read it in the app (UTC); null = unread.</summary>
        public DateTime? ReadAt { get; set; }

        public ICollection<PushNotificationDelivery> Deliveries { get; set; } = [];
    }
}
