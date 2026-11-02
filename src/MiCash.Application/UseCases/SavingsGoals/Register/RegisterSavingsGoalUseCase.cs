using MiCash.Communication.Requests.SavingsGoals;
using MiCash.Communication.Responses.SavingsGoals;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.SavingsGoals;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.SavingsGoals.Register
{
    public interface IRegisterSavingsGoalUseCase
    {
        Task<ResponseRegisteredSavingsGoalJson> Execute ( RequestRegisterSavingsGoalJson request , CancellationToken cancellationToken );
    }

    public class RegisterSavingsGoalUseCase : IRegisterSavingsGoalUseCase
    {
        private readonly ISavingsGoalWriteOnlyRepository _writeOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBrazilClock _clock;

        public RegisterSavingsGoalUseCase (
            ISavingsGoalWriteOnlyRepository writeOnlyRepository ,
            ILoggedUser loggedUser ,
            IUnitOfWork unitOfWork ,
            IBrazilClock clock )
        {
            _writeOnlyRepository = writeOnlyRepository;
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<ResponseRegisteredSavingsGoalJson> Execute ( RequestRegisterSavingsGoalJson request , CancellationToken cancellationToken )
        {
            var today = _clock.Today;

            SavingsGoalMapper.ThrowIfInvalid(new ValidatorRegisterSavingsGoal(today).Validate(request));

            var user = await _loggedUser.Get();

            var goal = new SavingsGoal
            {
                UserId = user.Id ,
                Name = request.Name.Trim() ,
                TargetAmount = request.TargetAmount ,
                Deadline = request.Deadline ,
                Icon = request.Icon ,
                Color = request.Color
            };

            // "Já guardado" becomes a deposit so it also shows in the history.
            if ( request.InitialAmount is > 0 ) {
                goal.Movements.Add(new SavingsGoalMovement
                {
                    Type = SavingsMovementType.Deposit ,
                    Amount = request.InitialAmount.Value ,
                    Date = today
                });
            }

            await _writeOnlyRepository.AddAsync(goal , cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            return new ResponseRegisteredSavingsGoalJson { Id = goal.Id };
        }
    }
}
