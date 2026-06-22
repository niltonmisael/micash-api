using AutoMapper;
using MiCash.Communication.Pagination;
using MiCash.Communication.Responses;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.Expenses.GetAll
{
    public class GetAllExpensesUseCase : IGetAllExpensesUseCase
    {
        private readonly IExpensesReadOnlyRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILoggedUser _loggedUser;
        public GetAllExpensesUseCase (IExpensesReadOnlyRepository repository, IMapper mapper, ILoggedUser loggedUser)
        {
            _repository = repository;
            _mapper = mapper;
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
                Data = _mapper.Map<IReadOnlyList<ResponseShortExpenseJson>>(result.Data) ,
                Page = request.Page ,
                PageSize = request.PageSize ,
                TotalRecords = result.TotalRecords
            };
        }
    }
}
