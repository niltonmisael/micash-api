using MiCash.Communication.Responses.FinancialGoals;

namespace MiCash.Application.UseCases.FinancialGoals.GetByPeriod
{
    public interface IGetFinancialGoalByPeriodUseCase
    {
        Task<ResponseFinancialGoalsJson> Execute (
            int month ,
            int year ,
            CancellationToken cancellationToken );

    }
}
