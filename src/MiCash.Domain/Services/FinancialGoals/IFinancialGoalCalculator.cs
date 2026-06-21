using MiCash.Domain.Entities;

namespace MiCash.Domain.Services.FinancialGoals
{
    public interface IFinancialGoalCalculator
    {
        FinancialGoalCalculation Calculate (
            FinancialGoal goal ,
            decimal currentAmount );
    }
}
