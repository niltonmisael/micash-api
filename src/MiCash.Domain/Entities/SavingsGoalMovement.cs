using MiCash.Domain.Enums;

namespace MiCash.Domain.Entities
{
    public class SavingsGoalMovement
    {
        public long Id { get; set; }

        public long SavingsGoalId { get; set; }

        public SavingsGoal SavingsGoal { get; set; } = default!;

        public SavingsMovementType Type { get; set; }

        /// <summary>Always positive; the sign comes from <see cref="Type"/>.</summary>
        public decimal Amount { get; set; }

        public DateOnly Date { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
