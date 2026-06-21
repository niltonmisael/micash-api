using MiCash.Domain.Enums;

namespace MiCash.Domain.Services.FinancialGoals
{
    public class FinancialGoalCalculation
    {
        public decimal CurrentAmount { get; init; }

        public decimal TargetAmount { get; init; }

        public decimal RemainingAmount { get; init; }

        public decimal PercentageUsed { get; init; }

        public FinancialGoalProgressStatus Status { get; init; }
    }
}
