using MiCash.Communication.Responses.FinancialGoals;

namespace MiCash.Application.UseCases.FinancialGols.GetById
{
    public interface IGetFinancialGoalByIdUseCase
    {
        Task<ResponseFinancialGoalJson> Execute ( long id , CancellationToken cancellationToken );
    }
}
