using MiCash.Communication.Requests.FinancialGoals;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.Cache.FinancialInvalidator;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.FinancialGoals.Update
{
    public class UpdateFinancialGoalUseCase : IUpdateFinancialGoalUseCase
    {
        private readonly IFinancialGoalUpdateOnlyRepository _financialGoalUpdateOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFinancialCacheInvalidator _financialCacheInvalidator;

        public UpdateFinancialGoalUseCase (
            IFinancialGoalUpdateOnlyRepository financialGoalUpdateOnlyRepository ,
            ILoggedUser loggedUser ,
            IUnitOfWork unitOfWork ,
            IFinancialCacheInvalidator financialCacheInvalidator )
        {
            _financialGoalUpdateOnlyRepository = financialGoalUpdateOnlyRepository;
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
            _financialCacheInvalidator = financialCacheInvalidator;
        }

        public async Task Execute ( long id , RequestUpdateFinancialGoalJson request , CancellationToken cancellationToken )
        {
            Validate(request);

            var user = await _loggedUser.Get();

            var financialGoal = await _financialGoalUpdateOnlyRepository
                .GetByIdAsync(user.Id , id , cancellationToken);

            if ( financialGoal is null ) {
                throw new NotFoundException(ResourceErrorMessages.FINANCIAL_GOAL_NOT_FOUND);
            }

            financialGoal.TargetAmount = request.TargetAmount;
            financialGoal.UpdatedAt = DateTime.UtcNow;

            _financialGoalUpdateOnlyRepository.Update(financialGoal);

            await _unitOfWork.Commit(cancellationToken);

            await _financialCacheInvalidator
                .InvalidateFinancialGoalAsync(
                    user.Id ,
                    financialGoal.Id);

            await _financialCacheInvalidator
                .InvalidateMonthAsync(
                    user.Id ,
                    financialGoal.Month ,
                    financialGoal.Year);
        }

        private static void Validate ( RequestUpdateFinancialGoalJson request )
        {
            var result = new ValidatorUpdateFinancialGoal().Validate(request);

            if ( result.IsValid == false ) {
                var errorMessages = result.Errors.Select(f => f.ErrorMessage).ToList();
                throw new ErrorOnValidationException(errorMessages);
            }
        }
    }
}
