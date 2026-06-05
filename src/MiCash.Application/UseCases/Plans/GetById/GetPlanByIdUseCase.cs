using AutoMapper;
using MiCash.Communication.Responses.Plans;
using MiCash.Domain.Repositories.Plans;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Plans.GetById
{
    public class GetPlanByIdUseCase : IGetPlanByIdUseCase
    {
        private readonly IPlansReadOnlyRepository _plansReadOnlyRepository;
        private readonly IMapper _mapper;

        public GetPlanByIdUseCase (
            IPlansReadOnlyRepository plansReadOnlyRepository,
            IMapper mapper)
        {
            _plansReadOnlyRepository = plansReadOnlyRepository;
            _mapper = mapper;
        }
        public async Task<ResponsePlanJson> ExecuteAsync ( long id , CancellationToken cancellationToken )
        {
            var plan = await _plansReadOnlyRepository.GetByIdAsync(id , cancellationToken);

            if ( plan == null ) {
                throw new NotFoundException(ResourceErrorMessages.PLAN_NOT_FOUND);
            }

            return _mapper.Map<ResponsePlanJson>(plan);
        }
    }
}
