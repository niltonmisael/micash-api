using AutoMapper;
using MiCash.Communication.Responses.FinancialGoals;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using System.Text.Json;

namespace MiCash.Application.UseCases.FinancialGols.GetById
{
    public class GetFinancialGoalByIdUseCase : IGetFinancialGoalByIdUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
        private readonly IMapper _mapper;
        private readonly IRedisCacheService _redisCacheService;

        public GetFinancialGoalByIdUseCase (
            ILoggedUser loggedUser,
            IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository,
            IMapper mapper,
            IRedisCacheService redisCacheService )
        {
            _loggedUser = loggedUser;
            _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
            _mapper = mapper;
            _redisCacheService = redisCacheService;
        }
        public async Task<ResponseFinancialGoalJson> Execute ( long id , CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var financialGoal = await _financialGoalReadOnlyRepository.GetByIdAsync(id , cancellationToken);

            if( financialGoal == null ) {
                throw new NotFoundException(ResourceErrorMessages.FINANCIAL_GOAL_NOT_FOUND);
            }

            await _redisCacheService.SetAsync(
                CacheKeys.FinancialGoalById(user.Id , financialGoal.Id) ,
                JsonSerializer.Serialize(financialGoal) ,
                TimeSpan.FromMinutes(2)
            );

            return _mapper.Map<ResponseFinancialGoalJson>(financialGoal);
        }
    }
}
