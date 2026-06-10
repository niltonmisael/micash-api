using MiCash.Domain.Enums;

namespace MiCash.Domain.Entities
{
    public class Subscription
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public User User { get; set; } = default!;
        public long PlanId { get; set; }
        public Plan Plan { get; set; } = default!;
        public string? PagarmeSubscriptionId { get; set; }
        public SubscriptionStatus Status { get; set; }
        public string? PagarmeCustomerId { get; set; }
        public string? PagarmeChargeId { get; set; }
        public bool CancelAtPeriodEnd { get; set; }
        public string? CancellationJobId { get; set; }
        public DateTime? CancellationScheduledFor { get; set; }
        public DateTime? CancellationRequestedAt { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
