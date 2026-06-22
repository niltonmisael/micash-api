using AutoMapper;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.Incomes;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Incomes;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Expirations;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Incomes.GetById
{
    public class GetIncomeByIdUseCase : IGetIncomeByIdUseCase
    {
        private readonly IIncomesReadOnlyRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILoggedUser _loggedUser;
        private readonly IRedisCacheService _redisCacheService;

        public GetIncomeByIdUseCase (
            IIncomesReadOnlyRepository repository,
            IMapper mapper,
            ILoggedUser loggedUser,
            IRedisCacheService redisCacheService)
        {
            _repository = repository;
            _mapper = mapper;
            _loggedUser = loggedUser;
            _redisCacheService = redisCacheService;
        }
        public async Task<ResponseIncomeJson> Execute ( long id , CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();

            var cacheKey = CacheKeys.IncomeById(loggedUser.Id , id);

            var cachedIncome =
                await _redisCacheService
                .GetAsync<ResponseIncomeJson>(cacheKey);

            if ( cachedIncome is not null ) {
                return cachedIncome;
            }

            var result = await _repository.GetByIdAsync(loggedUser.Id , id, cancellationToken);

            if ( result is null ) {

                throw new NotFoundException(ResourceErrorMessages.EXPENSE_NOT_FOUND);
            }

            var response = _mapper.Map<ResponseIncomeJson>(result);

            await _redisCacheService.SetAsync(
                cacheKey ,
                response ,
                CacheExpiration.Income);

            return response;
        }
    }
}
