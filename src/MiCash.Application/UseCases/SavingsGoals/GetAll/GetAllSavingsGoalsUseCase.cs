using MiCash.Communication.Responses.SavingsGoals;
using MiCash.Domain.Repositories.SavingsGoals;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.SavingsGoals.GetAll
{
    public interface IGetAllSavingsGoalsUseCase
    {
        Task<ResponseSavingsGoalsJson> Execute ( CancellationToken cancellationToken );
    }

    public class GetAllSavingsGoalsUseCase : IGetAllSavingsGoalsUseCase
    {
        private readonly ISavingsGoalReadOnlyRepository _readOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IBrazilClock _clock;

        public GetAllSavingsGoalsUseCase (
            ISavingsGoalReadOnlyRepository readOnlyRepository ,
            ILoggedUser loggedUser ,
            IBrazilClock clock )
        {
            _readOnlyRepository = readOnlyRepository;
            _loggedUser = loggedUser;
            _clock = clock;
        }

        public async Task<ResponseSavingsGoalsJson> Execute ( CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var today = _clock.Today;

            var goals = await _readOnlyRepository.GetAllWithBalanceAsync(user.Id , cancellationToken);

            return new ResponseSavingsGoalsJson
            {
                // In progress first, by deadline (no deadline last); then the completed ones.
                SavingsGoals = goals
                    .Select(row => SavingsGoalMapper.ToShort(row.Goal , row.SavedAmount , today))
                    .OrderBy(goal => goal.IsCompleted)
                    .ThenBy(goal => goal.Deadline is null)
                    .ThenBy(goal => goal.Deadline)
                    .ThenBy(goal => goal.Id)
                    .ToList()
            };
        }
    }
}
