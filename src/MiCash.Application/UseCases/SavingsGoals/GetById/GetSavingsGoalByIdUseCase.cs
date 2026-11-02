using MiCash.Communication.Responses.SavingsGoals;
using MiCash.Domain.Repositories.SavingsGoals;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.SavingsGoals.GetById
{
    public interface IGetSavingsGoalByIdUseCase
    {
        Task<ResponseSavingsGoalJson> Execute ( long id , CancellationToken cancellationToken );
    }

    public class GetSavingsGoalByIdUseCase : IGetSavingsGoalByIdUseCase
    {
        private readonly ISavingsGoalReadOnlyRepository _readOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IBrazilClock _clock;

        public GetSavingsGoalByIdUseCase (
            ISavingsGoalReadOnlyRepository readOnlyRepository ,
            ILoggedUser loggedUser ,
            IBrazilClock clock )
        {
            _readOnlyRepository = readOnlyRepository;
            _loggedUser = loggedUser;
            _clock = clock;
        }

        public async Task<ResponseSavingsGoalJson> Execute ( long id , CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var goal = await _readOnlyRepository.GetByIdWithMovementsAsync(user.Id , id , cancellationToken)
                ?? throw new NotFoundException(ResourceErrorMessages.SAVINGS_GOAL_NOT_FOUND);

            return SavingsGoalMapper.ToDetail(goal , _clock.Today);
        }
    }
}
