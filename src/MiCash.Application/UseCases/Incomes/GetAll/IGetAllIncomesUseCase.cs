using MiCash.Communication.Responses.Incomes;

namespace MiCash.Application.UseCases.Incomes.GetAll
{
    public interface IGetAllIncomesUseCase
    {
        Task<ResponseIncomesJson> Execute ( CancellationToken cancellationToken );
    }
}
