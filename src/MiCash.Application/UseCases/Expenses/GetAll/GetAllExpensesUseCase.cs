using MiCash.Application.Mappers;
using MiCash.Communication.Pagination;
using MiCash.Communication.Responses;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.Expenses.GetAll
{
    public class GetAllExpensesUseCase : IGetAllExpensesUseCase
    {
        private readonly IExpensesReadOnlyRepository _repository;
        private readonly ILoggedUser _loggedUser;
        public GetAllExpensesUseCase (IExpensesReadOnlyRepository repository, ILoggedUser loggedUser)
        {
            _repository = repository;
            _loggedUser = loggedUser;

        }
        public async Task<ResponsePagedJson<ResponseShortExpenseJson>> Execute (
            RequestPaginationJson request ,
            CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var result = await _repository.GetAllAsync(
                user.Id ,
                request.Page ,
                request.PageSize ,
                cancellationToken);

            return new ResponsePagedJson<ResponseShortExpenseJson>
            {
                Data = result.Data.Select(expense => expense.ToShortResponse()).ToList() ,
                Page = request.Page ,
                PageSize = request.PageSize ,
                TotalRecords = result.TotalRecords
            };
        }
    }
}
