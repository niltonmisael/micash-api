using MiCash.Application.UseCases.FinancialGoals.Alert;
using MiCash.Communication.Responses.FinancialGoals;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Expirations;
using MiCash.Domain.Services.FinancialGoals;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Domain.Services.Plans;

namespace MiCash.Application.UseCases.FinancialGoals.Alert
{
    public class GetFinancialGoalsAlertsUseCase
        : IGetFinancialGoalsAlertsUseCase
    {
        private readonly IFinancialGoalReadOnlyRepository _financialGoalReadOnlyRepository;
        private readonly IExpensesReadOnlyRepository _expenseReadOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IFinancialGoalCalculator _financialGoalCalculator;
        private readonly IRedisCacheService _redisCacheService;
        private readonly IUserPlanService _userPlanService;

        public GetFinancialGoalsAlertsUseCase (
            IFinancialGoalReadOnlyRepository financialGoalReadOnlyRepository ,
            IExpensesReadOnlyRepository expenseReadOnlyRepository ,
            ILoggedUser loggedUser,
            IFinancialGoalCalculator financialGoalCalculator ,
            IRedisCacheService redisCacheService ,
            IUserPlanService userPlanService )
        {
            _financialGoalReadOnlyRepository = financialGoalReadOnlyRepository;
            _expenseReadOnlyRepository = expenseReadOnlyRepository;
            _loggedUser = loggedUser;
            _financialGoalCalculator = financialGoalCalculator;
            _redisCacheService = redisCacheService;
            _userPlanService = userPlanService;
        }

        public async Task<ResponseFinancialGoalsAlertsJson> Execute (
            int month ,
            int year ,
            CancellationToken cancellationToken )
        {
            PeriodValidator.Validate(month , year);

            var user = await _loggedUser.Get();

            var cacheKey = CacheKeys.FinancialGoalsAlerts(
                user.Id ,
                month ,
                year);

            var cached = await _redisCacheService
                .GetAsync<ResponseFinancialGoalsAlertsJson>(cacheKey);

            if ( cached is not null ) {
                return await ForPlanAsync(cached , user , cancellationToken);
            }

            // DbContext is not thread-safe: queries must run sequentially.
            var goals = await _financialGoalReadOnlyRepository
                .GetByPeriodAsync(
                user.Id ,
                month ,
                year ,
                cancellationToken);

            var expenses = await _expenseReadOnlyRepository
                .GetByPeriodAsync(
                    user.Id ,
                    month ,
                    year ,
                    cancellationToken);

            var expensesByCategory = expenses
                .GroupBy(expense => expense.CategoryId)
                .ToDictionary(
                    group => group.Key ,
                    group => group.Sum(expense => expense.Amount));

            var response = new ResponseFinancialGoalsAlertsJson();

            foreach ( var goal in goals ) {
                expensesByCategory.TryGetValue(
                    goal.CategoryId ,
                    out var currentAmount);

                var calculation =
                    _financialGoalCalculator
                        .Calculate(goal , currentAmount);


                if ( calculation.Status == FinancialGoalProgressStatus.Normal ) {
                    continue;
                }


                response.Alerts.Add(
                    new ResponseFinancialGoalAlertJson
                    {
                        CategoryId = goal.CategoryId ,

                        CategoryName = goal.Category.Name ,

                        TargetAmount = calculation.TargetAmount ,

                        CurrentAmount = calculation.CurrentAmount ,

                        DifferenceAmount = calculation.RemainingAmount ,

                        Status = ( int ) calculation.Status ,

                        StatusDescription = calculation.Status.ToString() ,

                        Message = GenerateMessage(
                            calculation.Status ,
                            goal.Category.Name ,
                            calculation.PercentageUsed ,
                            calculation.RemainingAmount)
                    });
            }
            await _redisCacheService.SetAsync(
                cacheKey ,
                response ,
                CacheExpiration.FinancialGoals);

            return await ForPlanAsync(response , user , cancellationToken);
        }

        /// <summary>
        /// The cache holds every alert; the Free plan only gets the exceeded ones (the 80% warning is Premium).
        /// A new object, so the cached instance is never changed.
        /// </summary>
        private async Task<ResponseFinancialGoalsAlertsJson> ForPlanAsync (
            ResponseFinancialGoalsAlertsJson response ,
            Domain.Entities.User user ,
            CancellationToken cancellationToken )
        {
            var limits = await _userPlanService.GetLimitsAsync(user , cancellationToken);

            if ( limits.GoalWarningAlerts ) {
                return response;
            }

            return new ResponseFinancialGoalsAlertsJson
            {
                Alerts = response.Alerts
                    .Where(alert => alert.Status == ( int ) FinancialGoalProgressStatus.Exceeded)
                    .ToList()
            };
        }

        private static string GenerateMessage (
            FinancialGoalProgressStatus status ,
            string categoryName ,
            decimal percentageUsed ,
            decimal remainingAmount )
        {
            return status switch
            {
                FinancialGoalProgressStatus.Warning =>
                    $"Você já utilizou {Math.Round(percentageUsed , 0)}% da sua meta de {categoryName}.",

                FinancialGoalProgressStatus.Exceeded =>
                    $"Você ultrapassou sua meta de {categoryName} em R$ {Math.Abs(remainingAmount):F2}.",

                _ => string.Empty
            };
        }
    }
}
