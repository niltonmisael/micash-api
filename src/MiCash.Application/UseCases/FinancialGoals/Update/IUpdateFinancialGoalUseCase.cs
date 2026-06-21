using MiCash.Communication.Requests;
using MiCash.Communication.Requests.FinancialGoals;

namespace MiCash.Application.UseCases.FinancialGols.Update
{
    public interface IUpdateFinancialGoalUseCase
    {
        Task Execute ( long id , RequestUpdateFinancialGoalJson request , CancellationToken cancellationToken );
    }
}
