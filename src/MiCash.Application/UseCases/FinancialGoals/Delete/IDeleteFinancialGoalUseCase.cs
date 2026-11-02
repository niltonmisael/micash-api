namespace MiCash.Application.UseCases.FinancialGoals.Delete
{
    public interface IDeleteFinancialGoalUseCase
    {
        Task Execute ( long id , CancellationToken cancellationToken );
    }
}
