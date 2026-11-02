using MiCash.Communication.Requests.FinancialGoals;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.Cache.FinancialInvalidator;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.FinancialGoals.Copy
{
    public class CopyFinancialGoalsUseCase : ICopyFinancialGoalsUseCase
    {
        private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
        private readonly IFinancialGoalWriteOnlyRepository _financialGoalWriteOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFinancialCacheInvalidator _financialCacheInvalidator;

        public CopyFinancialGoalsUseCase (
            IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository ,
            IFinancialGoalWriteOnlyRepository financialGoalWriteOnlyRepository ,
            ILoggedUser loggedUser ,
            IUnitOfWork unitOfWork ,
            IFinancialCacheInvalidator financialCacheInvalidator )
        {
            _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
            _financialGoalWriteOnlyRepository = financialGoalWriteOnlyRepository;
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
            _financialCacheInvalidator = financialCacheInvalidator;
        }

        public async Task Execute (
            RequestCopyFinancialGoalsJson request ,
            CancellationToken cancellationToken )
        {
            Validate(request);

            var user = await _loggedUser.Get();

            var sourceGoals =
                await _financialGoalReadOnlyRepository
                    .GetByPeriodAsync(
                        user.Id ,
                        request.SourceMonth ,
                        request.SourceYear ,
                        cancellationToken);

            if ( sourceGoals.Count == 0 ) {
                throw new ErrorOnValidationException(
                    [ResourceErrorMessages.FINANCIAL_GOAL_SOURCE_PERIOD_EMPTY]);
            }

            var targetGoals =
                await _financialGoalReadOnlyRepository
                    .GetByPeriodAsync(
                        user.Id ,
                        request.TargetMonth ,
                        request.TargetYear ,
                        cancellationToken);

            if ( targetGoals.Count != 0 ) {
                throw new ErrorOnValidationException(
                    [ResourceErrorMessages.FINANCIAL_GOAL_TARGET_PERIOD_NOT_EMPTY]);
            }

            var now = DateTime.UtcNow;

            var financialGoals = sourceGoals
                .Where(goal => goal.Category.IsActive)
                .Select(goal => new FinancialGoal
                {
                    UserId = user.Id ,
                    CategoryId = goal.CategoryId ,
                    TargetAmount = goal.TargetAmount ,
                    Month = request.TargetMonth ,
                    Year = request.TargetYear ,
                    CreatedAt = now ,
                    UpdatedAt = now
                })
                .ToList();

            if ( financialGoals.Count == 0 ) {
                throw new ErrorOnValidationException(
                    [ResourceErrorMessages.FINANCIAL_GOAL_SOURCE_PERIOD_EMPTY]);
            }

            await _financialGoalWriteOnlyRepository
                .AddRangeAsync(
                    financialGoals ,
                    cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            await _financialCacheInvalidator
                .InvalidateMonthAsync(
                    user.Id ,
                    request.TargetMonth ,
                    request.TargetYear);
        }

        private static void Validate ( RequestCopyFinancialGoalsJson request )
        {
            var result = new ValidatorCopyFinancialGoals().Validate(request);

            if ( result.IsValid == false ) {
                var errorMessages = result.Errors.Select(f => f.ErrorMessage).ToList();
                throw new ErrorOnValidationException(errorMessages);
            }
        }
    }
}
