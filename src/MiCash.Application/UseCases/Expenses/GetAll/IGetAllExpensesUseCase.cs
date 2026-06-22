using MiCash.Communication.Pagination;
using MiCash.Communication.Responses;

namespace MiCash.Application.UseCases.Expenses.GetAll
{
    public interface IGetAllExpensesUseCase
    {
        Task<ResponsePagedJson<ResponseShortExpenseJson>> Execute ( RequestPaginationJson request , CancellationToken cancellationToken );
    }
}
