namespace MiCash.Application.UseCases.Expenses.Delete
{
    public interface IDeleteExpenseUseCase
    {
        Task Execute(long id, CancellationToken cancellationToken);
    }
}
