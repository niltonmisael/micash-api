using MiCash.Domain.Repositories.Dashboards.DTOs;

namespace MiCash.Domain.Repositories.Dashboards
{
    public interface IDashboardReadOnlyRepository
    {
        Task<decimal> GetTotalAmountExpensesByMonth ( long id , int year , int month , CancellationToken cancellationToken );
        Task<decimal> GetTotalAmountIncomesByMonth ( long id , int year , int month , CancellationToken cancellationToken );
        Task<IEnumerable<ExpenseCategorySummary>> GetExpensesByCategoryAsync ( long userId , int year , int month , CancellationToken cancellationToken );
        Task<IEnumerable<LatestExpense>> GetLatestExpensesAsync ( long userId , int count, CancellationToken cancellationToken );
    }
}
