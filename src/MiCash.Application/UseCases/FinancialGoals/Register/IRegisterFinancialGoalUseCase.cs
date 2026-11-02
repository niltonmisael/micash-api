using MiCash.Communication.Requests;
using MiCash.Communication.Requests.FinancialGoals;
using MiCash.Communication.Responses.FinancialGoals;

namespace MiCash.Application.UseCases.FinancialGoals.Register
{
    public interface IRegisterFinancialGoalUseCase
    {
        Task<ResponseRegisteredFinancialGoalJson> Execute ( RequestRegisterFinancialGoal request , CancellationToken cancellationToken );
    }
}
