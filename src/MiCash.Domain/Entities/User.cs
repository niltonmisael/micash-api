using MiCash.Domain.Enums;

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

        public string UserRole { get; set; } = Roles.PREMIUM;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Category> Categories { get; set; } = [];

        public ICollection<Expense> Expenses { get; set; } = [];
    }
}
