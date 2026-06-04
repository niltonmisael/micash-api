using MiCash.Communication.Responses.Incomes;

namespace MiCash.Application.UseCases.Incomes.GetById
{
    public interface IGetIncomeByIdUseCase
    {
        Task<ResponseIncomeJson> Execute ( long id , CancellationToken cancellationToken );
    }
}
