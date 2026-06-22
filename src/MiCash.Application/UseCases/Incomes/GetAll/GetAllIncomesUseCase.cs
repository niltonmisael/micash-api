using AutoMapper;
using MiCash.Communication.Pagination;
using MiCash.Communication.Responses.Incomes;
using MiCash.Domain.Repositories.Incomes;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.Incomes.GetAll
{
    public class GetAllIncomesUseCase : IGetAllIncomesUseCase
    {
        private readonly IIncomesReadOnlyRepository _repository;
        private readonly ILoggedUser _loggedUser;
        private readonly IMapper _mapper;

        public GetAllIncomesUseCase (
            IIncomesReadOnlyRepository repository,
            ILoggedUser loggedUser,
            IMapper mapper)
        {
            _repository = repository;
            _loggedUser = loggedUser;
            _mapper = mapper;
        }
        public async Task<ResponsePagedJson<ResponseIncomeJson>> Execute ( Communication.Pagination.RequestPaginationJson request , CancellationToken cancellationToken )
        {
            var userLogged = await _loggedUser.Get();

            var result = await _repository.GetAllAsync(userLogged,request.Page, request.PageSize, cancellationToken);

            return new ResponsePagedJson<ResponseIncomeJson>
            {
                Data = _mapper.Map<IReadOnlyList<ResponseIncomeJson>>(result.Data) ,
                Page = request.Page ,
                PageSize = request.PageSize ,
                TotalRecords = result.TotalRecords
            };
        }
    }
}
