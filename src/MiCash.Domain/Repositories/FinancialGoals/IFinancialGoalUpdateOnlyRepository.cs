using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.FinancialGoals
{
    public interface IFinancialGoalUpdateOnlyRepository
    {
        void Update (
            FinancialGoal goal );
    }
}
