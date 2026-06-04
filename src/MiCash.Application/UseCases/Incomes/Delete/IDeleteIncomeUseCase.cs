namespace MiCash.Application.UseCases.Incomes.Delete
{
    public interface IDeleteIncomeUseCase
    {
        Task Execute ( long id , CancellationToken cancellationToken );
    }
}
