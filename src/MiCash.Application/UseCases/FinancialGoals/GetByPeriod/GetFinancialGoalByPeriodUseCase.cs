using MiCash.Communication.Responses.FinancialGoals;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Expirations;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.FinancialGoals.GetByPeriod
{
    public class GetFinancialGoalByPeriodUseCase : IGetFinancialGoalByPeriodUseCase
    {
        private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IRedisCacheService _redisCacheService;

        public GetFinancialGoalByPeriodUseCase (
            IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository,
            ILoggedUser loggedUser,
            IRedisCacheService redisCacheService)
        {
            _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
            _loggedUser = loggedUser;
            _redisCacheService = redisCacheService;
        }
        public async Task<ResponseFinancialGoalsJson> Execute (
            int month ,
            int year ,
            CancellationToken cancellationToken )
        {
            PeriodValidator.Validate(month , year);

            var user =
                await _loggedUser.Get();

            var cacheKey = CacheKeys.FinancialGoalsByPeriod(user.Id, month, year);

            var cached =
                await _redisCacheService
                .GetAsync<ResponseFinancialGoalsJson>(cacheKey);

            if ( cached is not null ) {
                return cached;
            }

            var goals =
                await _financialGoalReadOnlyRepository
                    .GetByPeriodAsync(
                        user.Id ,
                        month ,
                        year ,
                        cancellationToken);

            var response = new ResponseFinancialGoalsJson
            {
                FinancialGoals = goals
                    .Select(goal => new ResponseFinancialGoalJson
                    {
                        Id = goal.Id ,
                        CategoryId = goal.CategoryId ,
                        CategoryName = goal.Category.Name ,
                        TargetAmount = goal.TargetAmount ,
                        Month = goal.Month ,
                        Year = goal.Year
                    })
                    .ToList()
            };

            await _redisCacheService.SetAsync(
                cacheKey ,
                response ,
                CacheExpiration.FinancialGoals);

            return response;
        }
    }
}
