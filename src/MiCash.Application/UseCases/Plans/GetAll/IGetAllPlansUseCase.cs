using MiCash.Communication.Responses.Plans;

namespace MiCash.Application.UseCases.Plans.GetAll
{
    public interface IGetAllPlansUseCase
    {
        Task<IEnumerable<ResponsePlansJson>> ExecuteAsync ( CancellationToken cancellationToken );
    }
}
