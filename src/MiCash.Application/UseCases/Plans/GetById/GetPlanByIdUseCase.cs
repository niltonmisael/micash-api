using MiCash.Application.Mappers;
using MiCash.Communication.Responses.Plans;
using MiCash.Domain.Repositories.Plans;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Expirations;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Plans.GetById
{
    public class GetPlanByIdUseCase : IGetPlanByIdUseCase
    {
        private readonly IPlansReadOnlyRepository _plansReadOnlyRepository;
        private readonly IRedisCacheService _redisCacheService;

        public GetPlanByIdUseCase (
            IPlansReadOnlyRepository plansReadOnlyRepository,
            IRedisCacheService redisCacheService)
        {
            _plansReadOnlyRepository = plansReadOnlyRepository;
            _redisCacheService = redisCacheService;
        }
        public async Task<ResponsePlanJson> ExecuteAsync ( long id , CancellationToken cancellationToken )
        {
            var cacheKey = CacheKeys.PlanById(id);

            var cached = await _redisCacheService.GetAsync<ResponsePlanJson>(cacheKey);

            if (cached != null) {
                return cached;
            }
            var plan = await _plansReadOnlyRepository.GetByIdAsync(id , cancellationToken);

            if ( plan == null ) {
                throw new NotFoundException(ResourceErrorMessages.PLAN_NOT_FOUND);
            }

            var response = plan.ToResponse();

            await _redisCacheService.SetAsync(
                cacheKey, 
                response,
                CacheExpiration.Plan);

            return response;
        }
    }
}
