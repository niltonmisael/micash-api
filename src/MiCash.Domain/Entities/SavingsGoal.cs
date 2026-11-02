namespace MiCash.Domain.Entities
{
    /// <summary>
    /// "Meta de economia" (cofrinho): a target amount the user saves towards through deposits and
    /// withdrawals. Unrelated to <see cref="FinancialGoal"/>, which is a monthly spending limit.
    /// The saved amount is never stored: it is always derived from <see cref="Movements"/>.
    /// </summary>
    public class SavingsGoal
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public User User { get; set; } = default!;

        public string Name { get; set; } = string.Empty;

        public decimal TargetAmount { get; set; }

        /// <summary>Last day of the chosen month; null = no deadline.</summary>
        public DateOnly? Deadline { get; set; }

        /// <summary>Tabler icon name in kebab-case, same convention as <see cref="Category.Icon"/>.</summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>Hex color, same convention as <see cref="Category.Color"/>.</summary>
        public string Color { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<SavingsGoalMovement> Movements { get; set; } = [];
    }
}
