using MiCash.Domain.Enums;

namespace MiCash.Domain.Entities
{
    public class Expense
    {
        public long Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime ExpenseDate { get; set; }

        public decimal Amount { get; set; }

        public PaymentType PaymentType { get; set; }

        public ExpenseStatus Status { get; set; }

        public long CategoryId { get; set; }

        public Category Category { get; set; } = default!;

        public long UserId { get; set; }

        public User User { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}
