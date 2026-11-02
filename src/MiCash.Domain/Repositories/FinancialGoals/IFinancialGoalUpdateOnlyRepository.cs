using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.FinancialGoals
{
    public interface IFinancialGoalUpdateOnlyRepository
    {
        /// <summary>
        /// Returns a tracked entity owned by the user, ready to be updated or deleted.
        /// </summary>
        Task<FinancialGoal?> GetByIdAsync (
            long userId ,
            long id ,
            CancellationToken cancellationToken );

        void Update (
            FinancialGoal goal );
    }
}
