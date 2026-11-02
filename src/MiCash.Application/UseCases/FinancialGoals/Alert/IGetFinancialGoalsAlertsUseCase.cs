using MiCash.Communication.Responses.FinancialGoals;

namespace MiCash.Application.UseCases.FinancialGoals.Alert
{
    public interface IGetFinancialGoalsAlertsUseCase
    {
        Task<ResponseFinancialGoalsAlertsJson> Execute (
            int month ,
            int year ,
            CancellationToken cancellationToken );
    }
}
