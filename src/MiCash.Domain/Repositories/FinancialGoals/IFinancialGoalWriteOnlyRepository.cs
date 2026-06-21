using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.FinancialGoals
{
    public interface IFinancialGoalWriteOnlyRepository
    {
        Task AddAsync (
            FinancialGoal goal ,
            CancellationToken cancellationToken );

        Task AddRangeAsync (
            IEnumerable<FinancialGoal> goals ,
            CancellationToken cancellationToken );

        void Delete (
            FinancialGoal goal );
    }
}
