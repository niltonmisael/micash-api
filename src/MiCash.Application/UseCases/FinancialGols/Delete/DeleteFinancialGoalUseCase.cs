using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.FinancialGols.Delete
{
    public class DeleteFinancialGoalUseCase : IDeleteFinancialGoalUseCase
    {
        private readonly IFinancialGoalWriteOnlyRepository _financialGoalWriteOnlyRepository;
        private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteFinancialGoalUseCase (
            IFinancialGoalWriteOnlyRepository financialGoalWriteOnlyRepository,
            IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository,
            ILoggedUser loggedUser,
            IUnitOfWork unitOfWork)
        {
            _financialGoalWriteOnlyRepository = financialGoalWriteOnlyRepository;
            _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
        }
        public async Task Execute ( long id , CancellationToken cancellationToken )
        {
            var user  = await _loggedUser.Get();

            var financialGoal = await _financialGoalReadOnlyRepository.GetByIdAsync(id , cancellationToken);

            if( financialGoal == null ) {

                throw new NotFoundException(ResourceErrorMessages.FINANCIAL_GOAL_NOT_FOUND);
            }

            _financialGoalWriteOnlyRepository.Delete(financialGoal);

            await _unitOfWork.Commit(cancellationToken);
        }
    }
}
