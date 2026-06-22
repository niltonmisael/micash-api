using MiCash.Communication.Pagination;
using MiCash.Communication.Responses.Incomes;

namespace MiCash.Application.UseCases.Incomes.GetAll
{
    public interface IGetAllIncomesUseCase
    {
        Task<ResponsePagedJson<ResponseIncomeJson>> Execute ( RequestPaginationJson request , CancellationToken cancellationToken );
    }
}
