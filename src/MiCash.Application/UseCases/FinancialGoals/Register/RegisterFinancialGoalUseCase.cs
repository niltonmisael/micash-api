using AutoMapper;
using MiCash.Communication.Requests.FinancialGoals;
using MiCash.Communication.Responses.FinancialGoals;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.FinancialInvalidator;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.FinancialGols.Register
{
    public class RegisterFinancialGoalUseCase : IRegisterFinancialGoalUseCase
    {
        private readonly IFinancialGoalWriteOnlyRepository _financialGoalWriteOnlyRepository;
        private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFinancialCacheInvalidator _financialCacheInvalidator;

        public RegisterFinancialGoalUseCase (
            IFinancialGoalWriteOnlyRepository financialGoalWriteOnlyRepository,
            IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository,
            ILoggedUser loggedUser,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IFinancialCacheInvalidator financialCacheInvalidator)
        {
            _financialGoalWriteOnlyRepository = financialGoalWriteOnlyRepository;
            _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
            _loggedUser = loggedUser;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _financialCacheInvalidator = financialCacheInvalidator;
        }

        public async Task<ResponseRegisteredFinancialGoalJson> Execute ( 
            RequestRegisterFinancialGoal request , CancellationToken cancellationToken )
        {
            Validate(request);

            var user = await _loggedUser.Get();

            var alreadyExists = await _financialGoalReadOnlyRepository
                .ExistsAsync(user.Id , request.CategoryId , request.Month , request.Year , cancellationToken);

            if( alreadyExists ) {
                throw new BadRequestException([ResourceErrorMessages.CATEGORY_MONTHLY_GOAL_DUPLICATED]);
            }

            var financialGoal = _mapper.Map<FinancialGoal>( request );
            financialGoal.UserId = user.Id;

            await _financialGoalWriteOnlyRepository.AddAsync(financialGoal , cancellationToken);

            await _unitOfWork.Commit( cancellationToken );

            await _financialCacheInvalidator
                .InvalidateMonthAsync(
                user.Id ,
                financialGoal.Month ,
                financialGoal.Year);

            await _financialCacheInvalidator
                .InvalidateFinancialGoalAsync(
                user.Id ,
                financialGoal.Id);

            return _mapper.Map<ResponseRegisteredFinancialGoalJson>(financialGoal);
        }

        private void Validate(RequestRegisterFinancialGoal request )
        {
            var validator = new ValidatorFinancialGoal();

            var result = validator.Validate( request );

            if ( result.IsValid == false ) {

                var errorMessage = result.Errors.Select(f => f.ErrorMessage).ToList();

                throw new ErrorOnValidationException(errorMessage);

            }
        }
    }
}
