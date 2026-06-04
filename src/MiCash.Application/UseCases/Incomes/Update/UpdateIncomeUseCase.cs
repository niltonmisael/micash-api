using AutoMapper;
using MiCash.Communication.Requests.Incomes;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Incomes;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Incomes.Update
{
    public class UpdateIncomeUseCase : IUpdateIncomeUseCase
    {
        private readonly IIncomesUpdateOnlyRepository _incomesUpdateOnlyRepository;
        private readonly IIncomesReadOnlyRepository _incomesReadOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILoggedUser _loggedUser;

        public UpdateIncomeUseCase (
            IIncomesUpdateOnlyRepository incomesUpdateOnlyRepository ,
            IIncomesReadOnlyRepository incomesReadOnlyRepository ,
            IUnitOfWork unitOfWork ,
            IMapper mapper ,
            ILoggedUser loggedUser )
        {
            _incomesUpdateOnlyRepository = incomesUpdateOnlyRepository;
            _incomesReadOnlyRepository = incomesReadOnlyRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _loggedUser = loggedUser;
        }
        public async Task Execute ( long id , RequestIncomeJson request , CancellationToken cancellationToken )
        {
            Validate(request);

            var loggedUser = await _loggedUser.Get();

            var income = await _incomesReadOnlyRepository.GetByIdAsync(loggedUser.Id, id , cancellationToken);

            if( income == null ) {
                throw new NotFoundException(ResourceErrorMessages.INCOME_NOT_FOUND);
            }

            _mapper.Map(request , income);

            await _incomesUpdateOnlyRepository.UpdateAsync(income , cancellationToken);

            await _unitOfWork.Commit(cancellationToken);
        }

        private void Validate ( RequestIncomeJson request )
        {
            var validator = new IncomeValidator();
            var result = validator.Validate(request);
            if ( result.IsValid == false ) {
                var errorMessages = result.Errors.Select(e => e.ErrorMessage).ToList();
                throw new ErrorOnValidationException(errorMessages);
            }
        }
    }
}
