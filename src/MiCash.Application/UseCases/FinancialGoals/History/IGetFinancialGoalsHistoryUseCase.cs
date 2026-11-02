using MiCash.Communication.Pagination;
using MiCash.Communication.Responses.FinancialGoals;

namespace MiCash.Application.UseCases.FinancialGoals.History
{
    public interface IGetFinancialGoalsHistoryUseCase
    {
        Task<ResponsePagedJson<ResponseFinancialGoalJson>> Execute (
            RequestPaginationJson request ,
            CancellationToken cancellationToken );
    }
}
