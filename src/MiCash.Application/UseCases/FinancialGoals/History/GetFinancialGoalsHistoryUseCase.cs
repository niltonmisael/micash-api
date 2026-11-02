using AutoMapper;
using MiCash.Communication.Pagination;
using MiCash.Communication.Responses.FinancialGoals;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.FinancialGoals.History
{
    public class GetFinancialGoalsHistoryUseCase : IGetFinancialGoalsHistoryUseCase
    {
        private const int MAX_PAGE_SIZE = 100;

        private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IMapper _mapper;

        public GetFinancialGoalsHistoryUseCase (
            IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository ,
            ILoggedUser loggedUser ,
            IMapper mapper)
        {
            _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
            _loggedUser = loggedUser;
            _mapper = mapper;
        }

        public async Task<ResponsePagedJson<ResponseFinancialGoalJson>> Execute (
            RequestPaginationJson request ,
            CancellationToken cancellationToken )
        {
            var page = Math.Max(request.Page , 1);
            var pageSize = Math.Clamp(request.PageSize , 1 , MAX_PAGE_SIZE);

            var user = await _loggedUser.Get();

            var result =
                await _financialGoalReadOnlyRepository
                    .GetHistoryByUserIdPagedAsync(
                        user.Id ,
                        page ,
                        pageSize ,
                        cancellationToken);

            var response = new ResponsePagedJson<ResponseFinancialGoalJson>
            {
                Data = _mapper.Map<IReadOnlyList<ResponseFinancialGoalJson>>( result.Goals) ,

                Page = page ,

                PageSize = pageSize ,

                TotalRecords = result.TotalRecords
            };

            return response;
        }
    }
}