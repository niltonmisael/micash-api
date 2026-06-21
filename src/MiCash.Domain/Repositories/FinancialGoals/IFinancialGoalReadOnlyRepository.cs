using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.FinancialGoals
{
    public interface IFinancialGoalReadOnlyRepository
    {
        Task<bool> ExistsAsync ( 
            long userId , 
            long categoryId , 
            int month , 
            int year , 
            CancellationToken cancellationToken );

        Task<FinancialGoal?> GetByIdAsync (
            long id ,
            CancellationToken cancellationToken );

        Task<List<FinancialGoal>> GetByPeriodAsync (
            long userId ,
            int month ,
            int year ,
            CancellationToken cancellationToken );

        Task<List<FinancialGoal>> GetByPeriodAndCategoriesAsync (
            long userId ,
            int month ,
            int year ,
            CancellationToken cancellationToken );

        Task<IReadOnlyCollection<FinancialGoal>> GetHistoryByUserIdAsync (
            long userId ,
            CancellationToken cancellationToken );

        Task<(IReadOnlyCollection<FinancialGoal> Goals , int TotalRecords)> GetHistoryByUserIdPagedAsync (
            long userId ,
            int page ,
            int pageSize ,
            CancellationToken cancellationToken );
    }
}
