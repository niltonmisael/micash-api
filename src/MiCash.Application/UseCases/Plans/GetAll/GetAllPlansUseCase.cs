using AutoMapper;
using MiCash.Communication.Responses.Plans;
using MiCash.Domain.Repositories.Plans;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Plans.GetAll
{
    public class GetAllPlansUseCase : IGetAllPlansUseCase
    {
        private readonly IPlansReadOnlyRepository _plansReadOnlyRepository;
        private readonly IMapper _mapper;

        public GetAllPlansUseCase (
            IPlansReadOnlyRepository plansReadOnlyRepository,
            IMapper mapper)
        {
            _plansReadOnlyRepository = plansReadOnlyRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ResponsePlansJson>> ExecuteAsync ( CancellationToken cancellationToken )
        {
            var plans = await _plansReadOnlyRepository.GetAllActiveAsync(cancellationToken);

            if ( plans == null ) {
                throw new NotFoundException(ResourceErrorMessages.PLAN_NOT_FOUND);
            }

            return _mapper.Map<IEnumerable<ResponsePlansJson>>(plans);
        }
    }
}
