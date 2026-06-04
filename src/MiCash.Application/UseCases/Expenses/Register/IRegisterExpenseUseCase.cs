using MiCash.Communication.Requests;
using MiCash.Communication.Responses;

namespace MiCash.Application.UseCases.Expenses.Register
{
    public interface IRegisterExpenseUseCase
    {
        Task<ResponseRegisteredExpenseJson> Execute (RequestExpenseJson request, CancellationToken cancellationToken );
    }
}
