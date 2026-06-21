using AutoMapper;
using MiCash.Communication.Pagination;
using MiCash.Communication.Responses.FinancialGoals;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.LoggedUser;
using System.Text.Json;

namespace MiCash.Application.UseCases.FinancialGols.History
{
    public class GetFinancialGoalsHistoryUseCase : IGetFinancialGoalsHistoryUseCase
    {
        private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IMapper _mapper;
        private readonly IRedisCacheService _redisCacheService;

        public GetFinancialGoalsHistoryUseCase (
            IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository ,
            ILoggedUser loggedUser ,
            IMapper mapper ,
            IRedisCacheService redisCacheService)
        {
            _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
            _loggedUser = loggedUser;
            _mapper = mapper;
            _redisCacheService = redisCacheService;
        }

        public async Task<ResponsePagedJson<ResponseFinancialGoalJson>> Execute (
            RequestPaginationJson request ,
            CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var result =
                await _financialGoalReadOnlyRepository
                    .GetHistoryByUserIdPagedAsync(
                        user.Id ,
                        request.Page ,
                        request.PageSize ,
                        cancellationToken);

            await _redisCacheService.SetAsync(
                CacheKeys.FinancialGoalsHistory(user.Id,request.Page, request.PageSize),
                JsonSerializer.Serialize(result),
                TimeSpan.FromMinutes(15)
            );

            return new ResponsePagedJson<ResponseFinancialGoalJson>
            {
                Data = _mapper.Map<IReadOnlyList<ResponseFinancialGoalJson>>( result.Goals) ,

                Page = request.Page ,

                PageSize = request.PageSize ,

                TotalRecords = result.TotalRecords
            };
        }
    }
}