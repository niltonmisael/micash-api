using MiCash.Application.Mappers;
using MiCash.Communication.Responses.FinancialGoals;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Expirations;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.FinancialGoals.GetById
{
    public class GetFinancialGoalByIdUseCase : IGetFinancialGoalByIdUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
        private readonly IRedisCacheService _redisCacheService;

        public GetFinancialGoalByIdUseCase (
            ILoggedUser loggedUser,
            IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository,
            IRedisCacheService redisCacheService )
        {
            _loggedUser = loggedUser;
            _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
            _redisCacheService = redisCacheService;
        }
        public async Task<ResponseFinancialGoalJson> Execute (
            long id ,
            CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var cacheKey = CacheKeys.FinancialGoalById(
                user.Id ,
                id);

            var cached =
                await _redisCacheService
                    .GetAsync<ResponseFinancialGoalJson>(cacheKey);

            if ( cached is not null ) {
                return cached;
            }

            var financialGoal =
                await _financialGoalReadOnlyRepository
                    .GetByIdAsync(
                        user.Id ,
                        id ,
                        cancellationToken);

            if ( financialGoal is null ) {
                throw new NotFoundException(
                    ResourceErrorMessages.FINANCIAL_GOAL_NOT_FOUND);
            }

            var response = financialGoal.ToResponse();

            await _redisCacheService.SetAsync(
                cacheKey ,
                response ,
                CacheExpiration.FinancialGoals);

            return response;
        }
    }
}
