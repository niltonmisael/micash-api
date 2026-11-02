using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.Cache.FinancialInvalidator;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.FinancialGoals.Delete
{
    public class DeleteFinancialGoalUseCase : IDeleteFinancialGoalUseCase
    {
        private readonly IFinancialGoalWriteOnlyRepository _financialGoalWriteOnlyRepository;
        private readonly IFinancialGoalUpdateOnlyRepository _financialGoalUpdateOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFinancialCacheInvalidator _financialCacheInvalidator;

        public DeleteFinancialGoalUseCase (
            IFinancialGoalWriteOnlyRepository financialGoalWriteOnlyRepository ,
            IFinancialGoalUpdateOnlyRepository financialGoalUpdateOnlyRepository ,
            ILoggedUser loggedUser ,
            IUnitOfWork unitOfWork ,
            IFinancialCacheInvalidator financialCacheInvalidator )
        {
            _financialGoalWriteOnlyRepository = financialGoalWriteOnlyRepository;
            _financialGoalUpdateOnlyRepository = financialGoalUpdateOnlyRepository;
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
            _financialCacheInvalidator = financialCacheInvalidator;
        }

        public async Task Execute ( long id , CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var financialGoal = await _financialGoalUpdateOnlyRepository
                .GetByIdAsync(user.Id , id , cancellationToken);

            if ( financialGoal is null ) {
                throw new NotFoundException(ResourceErrorMessages.FINANCIAL_GOAL_NOT_FOUND);
            }

            _financialGoalWriteOnlyRepository.Delete(financialGoal);

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
