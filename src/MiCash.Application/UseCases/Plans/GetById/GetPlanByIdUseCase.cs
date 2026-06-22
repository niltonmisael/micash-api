using AutoMapper;
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
        private readonly IMapper _mapper;
        private readonly IRedisCacheService _redisCacheService;

        public GetPlanByIdUseCase (
            IPlansReadOnlyRepository plansReadOnlyRepository,
            IMapper mapper,
            IRedisCacheService redisCacheService)
        {
            _plansReadOnlyRepository = plansReadOnlyRepository;
            _mapper = mapper;
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

            var response = _mapper.Map<ResponsePlanJson>(plan);

            await _redisCacheService.SetAsync(
                cacheKey, 
                response,
                CacheExpiration.Plan);

            return response;
        }
    }
}
