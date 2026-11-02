using MiCash.Communication.Requests.SavingsGoals;
using MiCash.Communication.Responses.SavingsGoals;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.SavingsGoals;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.SavingsGoals.Movements
{
    /// <summary>Deposit ("Guardar dinheiro") or withdrawal ("Retirar") on a savings goal.</summary>
    public interface IRegisterSavingsMovementUseCase
    {
        Task<ResponseRegisteredSavingsMovementJson> Execute (
            long savingsGoalId ,
            SavingsMovementType type ,
            RequestSavingsMovementJson request ,
            CancellationToken cancellationToken );
    }

    public class RegisterSavingsMovementUseCase : IRegisterSavingsMovementUseCase
    {
        private readonly ISavingsGoalUpdateOnlyRepository _updateOnlyRepository;
        private readonly ISavingsGoalReadOnlyRepository _readOnlyRepository;
        private readonly ISavingsGoalWriteOnlyRepository _writeOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBrazilClock _clock;

        public RegisterSavingsMovementUseCase (
            ISavingsGoalUpdateOnlyRepository updateOnlyRepository ,
            ISavingsGoalReadOnlyRepository readOnlyRepository ,
            ISavingsGoalWriteOnlyRepository writeOnlyRepository ,
            ILoggedUser loggedUser ,
            IUnitOfWork unitOfWork ,
            IBrazilClock clock )
        {
            _updateOnlyRepository = updateOnlyRepository;
            _readOnlyRepository = readOnlyRepository;
            _writeOnlyRepository = writeOnlyRepository;
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<ResponseRegisteredSavingsMovementJson> Execute (
            long savingsGoalId ,
            SavingsMovementType type ,
            RequestSavingsMovementJson request ,
            CancellationToken cancellationToken )
        {
            SavingsGoalMapper.ThrowIfInvalid(new ValidatorSavingsMovement(_clock.Today).Validate(request));

            var user = await _loggedUser.Get();

            // Optimistic concurrency: if another movement/edit of the same goal commits in between,
            // our commit fails as a whole (ConflictException) and we start over with fresh data,
            // so the balance check always runs against the latest balance.
            for ( var attempt = 1; ; attempt++ ) {
                try {
                    return await TryRegister(user.Id , savingsGoalId , type , request , cancellationToken);
                } catch ( ConflictException ) when ( attempt < MAX_ATTEMPTS ) {
                    // retry
                }
            }
        }

        public const int MAX_ATTEMPTS = 3;

        private async Task<ResponseRegisteredSavingsMovementJson> TryRegister (
            long userId ,
            long savingsGoalId ,
            SavingsMovementType type ,
            RequestSavingsMovementJson request ,
            CancellationToken cancellationToken )
        {
            // Read the goal (and its version) BEFORE the balance: any movement committed after this
            // point changes the version and makes our commit fail, so a stale balance can't be used.
            var goal = await _updateOnlyRepository.GetByIdAsync(userId , savingsGoalId , cancellationToken)
                ?? throw new NotFoundException(ResourceErrorMessages.SAVINGS_GOAL_NOT_FOUND);

            if ( type == SavingsMovementType.Withdrawal ) {
                var saved = await _readOnlyRepository.GetSavedAmountAsync(goal.Id , cancellationToken);

                if ( request.Amount > saved ) {
                    throw new ErrorOnValidationException([ResourceErrorMessages.SAVINGS_WITHDRAWAL_EXCEEDS_BALANCE]);
                }
            }

            var movement = new SavingsGoalMovement
            {
                SavingsGoalId = goal.Id ,
                Type = type ,
                Amount = request.Amount ,
                Date = request.Date
            };

            await _writeOnlyRepository.AddMovementAsync(movement , cancellationToken);

            // Touching the goal makes EF issue UPDATE ... WHERE xmin = @read in the same transaction.
            goal.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Commit(cancellationToken);

            return new ResponseRegisteredSavingsMovementJson { Id = movement.Id };
        }
    }
}
