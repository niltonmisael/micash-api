using MiCash.Communication.Requests.Incomes;

namespace MiCash.Application.UseCases.Incomes.Update
{
    public interface IUpdateIncomeUseCase
    {
        Task Execute ( long id , RequestIncomeJson request , CancellationToken cancellationToken );
    }
}
