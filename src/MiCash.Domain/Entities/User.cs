namespace MiCash.Domain.Entities
{
    public class User
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public Guid UserIdentifier { get; set; } = Guid.NewGuid();

        public string? PagarMeCustomerId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Category> Categories { get; set; } = [];

        public ICollection<Expense> Expenses { get; set; } = [];

        public ICollection<Income> Incomes { get; set; } = [];

        public ICollection<Subscription> Subscriptions { get; set; } = [];

    }
}
