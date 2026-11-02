using MiCash.Communication.Responses.FinancialGoals;

namespace MiCash.Application.UseCases.FinancialGoals.GetById
{
    public interface IGetFinancialGoalByIdUseCase
    {
        Task<ResponseFinancialGoalJson> Execute ( long id , CancellationToken cancellationToken );
    }
}
