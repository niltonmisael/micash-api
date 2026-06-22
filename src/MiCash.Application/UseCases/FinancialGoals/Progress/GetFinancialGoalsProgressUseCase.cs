using MiCash.Communication.Enums;
using MiCash.Communication.Responses.FinancialGoals;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Expirations;
using MiCash.Domain.Services.FinancialGoals;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.FinancialGols.Progress
{
    public class GetFinancialGoalsProgressUseCase
        : IGetFinancialGoalsProgressUseCase
    {
        private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
        private readonly IExpensesReadOnlyRepository _expenseReadOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IFinancialGoalCalculator _financialGoalCalculator;
        private readonly IRedisCacheService _redisCacheService;

        public GetFinancialGoalsProgressUseCase (
            IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository ,
            IExpensesReadOnlyRepository expenseReadOnlyRepository ,
            ILoggedUser loggedUser,
            IFinancialGoalCalculator financialGoalCalculator ,
            IRedisCacheService redisCacheService )
        {
            _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
            _expenseReadOnlyRepository = expenseReadOnlyRepository;
            _loggedUser = loggedUser;
            _financialGoalCalculator = financialGoalCalculator;
            _redisCacheService = redisCacheService;
        }

        public async Task<ResponseFinancialGoalsProgressJson> Execute (
            int month ,
            int year ,
            CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var cacheKey = CacheKeys.FinancialGoalsProgress(
                user.Id ,
                month ,
                year);

            var cached = await _redisCacheService
                .GetAsync<ResponseFinancialGoalsProgressJson>(cacheKey);

            if ( cached is not null ) {
                return cached;
            }

            var goalsTask =
                _financialGoalReadOnlyRepository
                .GetByPeriodAsync(
                    user.Id ,
                    month ,
                    year ,
                    cancellationToken);

            var expensesTask =
                _expenseReadOnlyRepository
                    .GetByPeriodAsync(
                        user.Id ,
                        month ,
                        year ,
                        cancellationToken);

            await Task.WhenAll(
                goalsTask ,
                expensesTask);

            var goals = await goalsTask;
            var expenses = await expensesTask;

            var expensesByCategory = expenses
                .GroupBy(expense => expense.CategoryId)
                .ToDictionary(
                    group => group.Key ,
                    group => group.Sum(expense => expense.Amount));

            var response = new ResponseFinancialGoalsProgressJson();

            foreach ( var goal in goals ) {

                expensesByCategory.TryGetValue(
                    goal.CategoryId ,
                    out var currentAmount);

                var calculation =
                    _financialGoalCalculator
                        .Calculate(goal , currentAmount);


                response.Goals.Add(
                    new ResponseFinancialGoalProgressJson
                    {
                        CategoryId = goal.CategoryId ,

                        CategoryName = goal.Category.Name ,

                        TargetAmount = calculation.TargetAmount ,

                        CurrentAmount = calculation.CurrentAmount ,

                        RemainingAmount = calculation.RemainingAmount ,

                        PercentageUsed = calculation.PercentageUsed ,

                        Status = ( int ) calculation.Status ,

                        StatusDescription = calculation.Status.ToString()
                    });
            }

            await _redisCacheService.SetAsync(
                CacheKeys.FinancialGoalsProgress(
                    user.Id ,
                    month ,
                    year) ,
                response ,
                CacheExpiration.FinancialGoals);

            return response;
        }

    }
}