using MiCash.Communication.Requests;

namespace MiCash.Application.UseCases.Expenses.Update
{
    public interface IUpdateExpenseUseCase
    {
        Task Execute ( long id , RequestExpenseJson request, CancellationToken cancellationToken );
    }
}
