namespace MiCash.Domain.Entities
{
    public class FinancialGoal
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public User User { get; set; } = default!;

        public long CategoryId { get; set; }

        public Category Category { get; set; } = default!;

        public decimal TargetAmount { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
