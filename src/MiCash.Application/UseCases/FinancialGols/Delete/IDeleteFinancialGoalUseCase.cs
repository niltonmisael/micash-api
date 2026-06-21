namespace MiCash.Application.UseCases.FinancialGols.Delete
{
    public interface IDeleteFinancialGoalUseCase
    {
        Task Execute ( long id , CancellationToken cancellationToken );
    }
}
