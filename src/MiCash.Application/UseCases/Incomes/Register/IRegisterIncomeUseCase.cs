using MiCash.Communication.Requests.Incomes;
using MiCash.Communication.Responses.Incomes;

namespace MiCash.Application.UseCases.Incomes.Register
{
    public interface IRegisterIncomeUseCase
    {
        Task<ResponseRegisteredIncomeJson> Execute ( RequestIncomeJson request , CancellationToken cancellationToken );
    }
}
