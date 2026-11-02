using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.SavingsGoals;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.SavingsGoals.Delete
{
    public interface IDeleteSavingsGoalUseCase
    {
        Task Execute ( long id , CancellationToken cancellationToken );
    }

    public class DeleteSavingsGoalUseCase : IDeleteSavingsGoalUseCase
    {
        private readonly ISavingsGoalUpdateOnlyRepository _updateOnlyRepository;
        private readonly ISavingsGoalWriteOnlyRepository _writeOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteSavingsGoalUseCase (
            ISavingsGoalUpdateOnlyRepository updateOnlyRepository ,
            ISavingsGoalWriteOnlyRepository writeOnlyRepository ,
            ILoggedUser loggedUser ,
            IUnitOfWork unitOfWork )
        {
            _updateOnlyRepository = updateOnlyRepository;
            _writeOnlyRepository = writeOnlyRepository;
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
        }

        public async Task Execute ( long id , CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var goal = await _updateOnlyRepository.GetByIdAsync(user.Id , id , cancellationToken)
                ?? throw new NotFoundException(ResourceErrorMessages.SAVINGS_GOAL_NOT_FOUND);

            _writeOnlyRepository.Delete(goal);

            await _unitOfWork.Commit(cancellationToken);
        }
    }
}
