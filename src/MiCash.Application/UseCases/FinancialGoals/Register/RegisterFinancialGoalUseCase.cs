using AutoMapper;
using MiCash.Communication.Requests.FinancialGoals;
using MiCash.Communication.Responses.FinancialGoals;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Categories;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.Cache.FinancialInvalidator;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Domain.Services.Plans;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.FinancialGoals.Register
{
    public class RegisterFinancialGoalUseCase : IRegisterFinancialGoalUseCase
    {
        private readonly IFinancialGoalWriteOnlyRepository _financialGoalWriteOnlyRepository;
        private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
        private readonly ICategoriesReadOnlyRepository _categoriesReadOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFinancialCacheInvalidator _financialCacheInvalidator;
        private readonly IUserPlanService _userPlanService;

        public RegisterFinancialGoalUseCase (
            IFinancialGoalWriteOnlyRepository financialGoalWriteOnlyRepository ,
            IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository ,
            ICategoriesReadOnlyRepository categoriesReadOnlyRepository ,
            ILoggedUser loggedUser ,
            IMapper mapper ,
            IUnitOfWork unitOfWork ,
            IFinancialCacheInvalidator financialCacheInvalidator ,
            IUserPlanService userPlanService )
        {
            _userPlanService = userPlanService;
            _financialGoalWriteOnlyRepository = financialGoalWriteOnlyRepository;
            _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
            _categoriesReadOnlyRepository = categoriesReadOnlyRepository;
            _loggedUser = loggedUser;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _financialCacheInvalidator = financialCacheInvalidator;
        }

        public async Task<ResponseRegisteredFinancialGoalJson> Execute (
            RequestRegisterFinancialGoal request ,
            CancellationToken cancellationToken )
        {
            Validate(request);

            var user = await _loggedUser.Get();

            var category = await _categoriesReadOnlyRepository
                .GetByIdAsync(user , request.CategoryId , cancellationToken);

            if ( category is null || category.IsActive == false ) {
                throw new NotFoundException(ResourceErrorMessages.CATEGORY_NOT_FOUND);
            }

            var alreadyExists = await _financialGoalReadOnlyRepository
                .ExistsAsync(user.Id , request.CategoryId , request.Month , request.Year , cancellationToken);

            if ( alreadyExists ) {
                throw new BadRequestException([ResourceErrorMessages.CATEGORY_MONTHLY_GOAL_DUPLICATED]);
            }

            var limits = await _userPlanService.GetLimitsAsync(user , cancellationToken);

            if ( limits.MaxFinancialGoalsPerMonth is int max ) {
                var goalsInMonth = await _financialGoalReadOnlyRepository
                    .GetByPeriodAsync(user.Id , request.Month , request.Year , cancellationToken);

                if ( goalsInMonth.Count >= max ) {
                    throw new PlanLimitException(string.Format(ResourceErrorMessages.PLAN_LIMIT_FINANCIAL_GOALS , max));
                }
            }

            var financialGoal = _mapper.Map<FinancialGoal>(request);
            financialGoal.UserId = user.Id;

            await _financialGoalWriteOnlyRepository.AddAsync(financialGoal , cancellationToken);
            await _unitOfWork.Commit(cancellationToken);

            await _financialCacheInvalidator
                .InvalidateMonthAsync(
                    user.Id ,
                    financialGoal.Month ,
                    financialGoal.Year);

            return _mapper.Map<ResponseRegisteredFinancialGoalJson>(financialGoal);
        }

        private static void Validate ( RequestRegisterFinancialGoal request )
        {
            var result = new ValidatorFinancialGoal().Validate(request);

            if ( result.IsValid == false ) {
                var errorMessages = result.Errors.Select(f => f.ErrorMessage).ToList();
                throw new ErrorOnValidationException(errorMessages);
            }
        }
    }
}
