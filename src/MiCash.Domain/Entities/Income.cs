using MiCash.Domain.Enums;

namespace MiCash.Domain.Entities
{
    public class Income
    {
        public long Id { get; private set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public DateOnly ReceivedAt { get; set; }
        public IncomeCategory Category { get; set; }
        public bool IsRecurring { get; set; }
        public long UserId { get; set; }
        public User User { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
