using MiCash.Communication.Requests.SavingsGoals;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.SavingsGoals;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.SavingsGoals.Update
{
    public interface IUpdateSavingsGoalUseCase
    {
        Task Execute ( long id , RequestUpdateSavingsGoalJson request , CancellationToken cancellationToken );
    }

    public class UpdateSavingsGoalUseCase : IUpdateSavingsGoalUseCase
    {
        private readonly ISavingsGoalUpdateOnlyRepository _updateOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBrazilClock _clock;

        public UpdateSavingsGoalUseCase (
            ISavingsGoalUpdateOnlyRepository updateOnlyRepository ,
            ILoggedUser loggedUser ,
            IUnitOfWork unitOfWork ,
            IBrazilClock clock )
        {
            _updateOnlyRepository = updateOnlyRepository;
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task Execute ( long id , RequestUpdateSavingsGoalJson request , CancellationToken cancellationToken )
        {
            SavingsGoalMapper.ThrowIfInvalid(new ValidatorUpdateSavingsGoal().Validate(request));

            var user = await _loggedUser.Get();

            var goal = await _updateOnlyRepository.GetByIdAsync(user.Id , id , cancellationToken)
                ?? throw new NotFoundException(ResourceErrorMessages.SAVINGS_GOAL_NOT_FOUND);

            // Only a NEW deadline must not be in the past: an expired goal stays editable.
            if ( request.Deadline != goal.Deadline &&
                !SavingsGoalRules.IsNotBeforeCurrentMonth(request.Deadline , _clock.Today) ) {
                throw new ErrorOnValidationException([ResourceErrorMessages.SAVINGS_GOAL_DEADLINE_IN_PAST]);
            }

            goal.Name = request.Name.Trim();
            goal.TargetAmount = request.TargetAmount;
            goal.Deadline = request.Deadline;
            goal.Icon = request.Icon;
            goal.Color = request.Color;
            goal.UpdatedAt = DateTime.UtcNow;

            // Tracked entity: SaveChanges detects the changes.
            await _unitOfWork.Commit(cancellationToken);
        }
    }
}
