namespace MiCash.Domain.Entities
{
    public class Category
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public long UserId { get; set; }

        public User User { get; set; } = default!;

        public ICollection<Expense> Expenses { get; set; } = [];

        public ICollection<FinancialGoal> FinancialGoals { get; set; } = [];
    }
}
