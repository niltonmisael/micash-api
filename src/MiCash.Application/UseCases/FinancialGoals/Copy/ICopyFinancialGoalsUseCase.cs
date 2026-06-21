using MiCash.Communication.Requests.FinancialGoals;

namespace MiCash.Application.UseCases.FinancialGols.Copy
{
    public interface ICopyFinancialGoalsUseCase
    {
        Task Execute ( RequestCopyFinancialGoalsJson request , CancellationToken cancellationToken );
    }
}
