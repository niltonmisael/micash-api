using MiCash.Communication.Responses.Plans;

namespace MiCash.Application.UseCases.Plans.GetById
{
    public interface IGetPlanByIdUseCase
    {
        Task<ResponsePlanJson> ExecuteAsync ( long id , CancellationToken cancellationToken );
    }
}
