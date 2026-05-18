using System.ComponentModel.DataAnnotations.Schema;

namespace MiCash.Domain.Entities
{
    public class Category
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("color")]
        public string Color { get; set; } = string.Empty;

        [Column("icon")]
        public string Icon { get; set; } = string.Empty;

        [Column("active")]
        public bool Active { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("user_id")]
        public long UserId { get; set; }
        public User User { get; set; } = default!;

        public ICollection<Expense> Expenses { get; set; }
    }
}
