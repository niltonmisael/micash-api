using MiCash.Communication.Requests.SavingsGoals;
using MiCash.Communication.Responses.SavingsGoals;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.SavingsGoals;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Domain.Services.Plans;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.SavingsGoals.Register
{
    public interface IRegisterSavingsGoalUseCase
    {
        Task<ResponseRegisteredSavingsGoalJson> Execute ( RequestRegisterSavingsGoalJson request , CancellationToken cancellationToken );
    }

    public class RegisterSavingsGoalUseCase : IRegisterSavingsGoalUseCase
    {
        private readonly ISavingsGoalWriteOnlyRepository _writeOnlyRepository;
        private readonly ISavingsGoalReadOnlyRepository _readOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBrazilClock _clock;
        private readonly IUserPlanService _userPlanService;

        public RegisterSavingsGoalUseCase (
            ISavingsGoalWriteOnlyRepository writeOnlyRepository ,
            ISavingsGoalReadOnlyRepository readOnlyRepository ,
            ILoggedUser loggedUser ,
            IUnitOfWork unitOfWork ,
            IBrazilClock clock ,
            IUserPlanService userPlanService )
        {
            _writeOnlyRepository = writeOnlyRepository;
            _readOnlyRepository = readOnlyRepository;
            _loggedUser = loggedUser;
            _unitOfWork = unitOfWork;
            _clock = clock;
            _userPlanService = userPlanService;
        }

        public async Task<ResponseRegisteredSavingsGoalJson> Execute ( RequestRegisterSavingsGoalJson request , CancellationToken cancellationToken )
        {
            var today = _clock.Today;

            SavingsGoalMapper.ThrowIfInvalid(new ValidatorRegisterSavingsGoal(today).Validate(request));

            var user = await _loggedUser.Get();

            var limits = await _userPlanService.GetLimitsAsync(user , cancellationToken);

            // Only creation is blocked: after a downgrade, existing goals stay usable.
            if ( limits.MaxSavingsGoals is int max ) {
                var existing = await _readOnlyRepository.GetAllWithBalanceAsync(user.Id , cancellationToken);

                if ( existing.Count >= max ) {
                    throw new PlanLimitException(string.Format(ResourceErrorMessages.PLAN_LIMIT_SAVINGS_GOALS , max));
                }
            }

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
