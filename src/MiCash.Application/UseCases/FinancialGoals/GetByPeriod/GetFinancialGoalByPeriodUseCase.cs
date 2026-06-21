using MiCash.Communication.Responses.FinancialGoals;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.FinancialGols.GetByPeriod
{
    public class GetFinancialGoalByPeriodUseCase : IGetFinancialGoalByPeriodUseCase
    {
        private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
        private readonly ILoggedUser _loggedUser;

        public GetFinancialGoalByPeriodUseCase (
            IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository,
            ILoggedUser loggedUser)
        {
            _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
            _loggedUser = loggedUser;
        }
        public async Task<ResponseFinancialGoalsJson> Execute (
            int month ,
            int year ,
            CancellationToken cancellationToken )
        {
            var user =
                await _loggedUser.Get();

            var goals =
                await _financialGoalReadOnlyRepository
                    .GetByPeriodAsync(
                        user.Id ,
                        month ,
                        year ,
                        cancellationToken);

            var response = new ResponseFinancialGoalsJson
            {
                FinancialGoals = goals
                    .Select(goal => new ResponseFinancialGoalJson
                    {
                        Id = goal.Id ,
                        CategoryId = goal.CategoryId ,
                        CategoryName = goal.Category.Name ,
                        TargetAmount = goal.TargetAmount ,
                        Month = goal.Month ,
                        Year = goal.Year
                    })
                    .ToList()
            };

            return response;
        }
    }
}
