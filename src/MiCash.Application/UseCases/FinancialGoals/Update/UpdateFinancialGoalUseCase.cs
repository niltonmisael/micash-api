using MiCash.Communication.Requests.FinancialGoals;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.FinancialInvalidator;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.FinancialGols.Update
{
    public class UpdateFinancialGoalUseCase : IUpdateFinancialGoalUseCase
    {
        private readonly IFinancialGoalUpdateOnlyRepository _financialGoalUpdateOnlyRepository;
        private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFinancialCacheInvalidator _financialCacheInvalidator;

        public UpdateFinancialGoalUseCase (
            IFinancialGoalUpdateOnlyRepository financialGoalUpdateOnlyRepository,
            IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository,
            ILoggedUser loggedUser,
            IUnitOfWork unitOfWork,
            IFinancialCacheInvalidator financialCacheInvalidator)
        {
            _financialGoalUpdateOnlyRepository = financialGoalUpdateOnlyRepository;
            _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
            _financialCacheInvalidator = financialCacheInvalidator;
        }
        public async Task Execute ( long id , RequestUpdateFinancialGoalJson request , CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var financialGoal = await _financialGoalReadOnlyRepository.GetByIdAsync(id , cancellationToken);

            if ( financialGoal is null ) {
                throw new NotFoundException(ResourceErrorMessages.FINANCIAL_GOAL_NOT_FOUND);
            }

            financialGoal.TargetAmount = request.TargetAmount;
            financialGoal.UpdatedAt = DateTime.UtcNow;

            _financialGoalUpdateOnlyRepository.Update( financialGoal );

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

    }
}
