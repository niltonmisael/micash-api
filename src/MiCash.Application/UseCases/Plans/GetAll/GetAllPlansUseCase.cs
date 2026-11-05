using MiCash.Application.Mappers;
using MiCash.Communication.Responses.Plans;
using MiCash.Domain.Repositories.Plans;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Expirations;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Plans.GetAll
{
    public class GetAllPlansUseCase : IGetAllPlansUseCase
    {
        private readonly IPlansReadOnlyRepository _plansReadOnlyRepository;
        private readonly IRedisCacheService _redisCacheService;

        public GetAllPlansUseCase (
            IPlansReadOnlyRepository plansReadOnlyRepository,
            IRedisCacheService redisCacheService)
        {
            _plansReadOnlyRepository = plansReadOnlyRepository;
            _redisCacheService = redisCacheService;
        }

        public async Task<IEnumerable<ResponsePlansJson>> ExecuteAsync ( CancellationToken cancellationToken )
        {
            var cacheKey = CacheKeys.Plans();

            var cached = await _redisCacheService.GetAsync<IEnumerable<ResponsePlansJson>>( cacheKey );

            if( cached != null ) {

                return cached;
            }

            var plans = await _plansReadOnlyRepository.GetAllActiveAsync(cancellationToken);

            if ( plans == null ) {
                throw new NotFoundException(ResourceErrorMessages.PLAN_NOT_FOUND);
            }

            var response = plans.Select(plan => plan.ToListItemResponse()).ToList();

            await _redisCacheService.SetAsync(
                cacheKey, 
                response,
                CacheExpiration.Plan);

            return response;
        }
    }
}
