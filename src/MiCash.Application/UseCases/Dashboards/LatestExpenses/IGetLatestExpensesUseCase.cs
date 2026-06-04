using MiCash.Communication.Responses.Dashboards;

namespace MiCash.Application.UseCases.Dashboards.LatestExpenses
{
    public interface IGetLatestExpensesUseCase
    {
        Task<List<ResponseLatestExpenseJson>> Execute ( int count , CancellationToken cancellationToken );
    }
}
