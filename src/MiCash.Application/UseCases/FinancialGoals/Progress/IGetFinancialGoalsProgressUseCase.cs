using MiCash.Communication.Responses.FinancialGoals;

namespace MiCash.Application.UseCases.FinancialGoals.Progress
{
    public interface IGetFinancialGoalsProgressUseCase
    {
        Task<ResponseFinancialGoalsProgressJson> Execute (
            int month ,
            int year ,
            CancellationToken cancellationToken );
    }
}
