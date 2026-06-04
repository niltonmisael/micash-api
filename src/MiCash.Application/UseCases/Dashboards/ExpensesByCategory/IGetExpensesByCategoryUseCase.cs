using MiCash.Communication.Responses.Dashboards;

namespace MiCash.Application.UseCases.Dashboards.ExpensesByCategory
{
    public interface IGetExpensesByCategoryUseCase
    {
        Task<List<ResponseExpenseCategorySummaryJson>> Execute ( int year , int month , CancellationToken cancellationToken );
    }
}
