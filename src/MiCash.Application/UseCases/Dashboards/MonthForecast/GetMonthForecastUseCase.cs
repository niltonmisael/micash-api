using MiCash.Communication.Responses.Dashboards;
using MiCash.Domain.Repositories.Dashboards;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Expirations;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.Forecast;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.Dashboards.MonthForecast
{
    public interface IGetMonthForecastUseCase
    {
        Task<ResponseMonthForecastJson> Execute ( CancellationToken cancellationToken );
    }

    /// <summary>
    /// Month-end forecast of the current month (Premium). Uses the same entries and month/day rule as
    /// daily-summary, so on the last day of the month ProjectedExpense equals its cumulative expense.
    /// </summary>
    public sealed class GetMonthForecastUseCase : IGetMonthForecastUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IDashboardReadOnlyRepository _dashboardRepository;
        private readonly IExpensesReadOnlyRepository _expensesRepository;
        private readonly IFinancialGoalReadOnlyRepository _goalsRepository;
        private readonly IRedisCacheService _cache;
        private readonly IBrazilClock _clock;

        public GetMonthForecastUseCase (
            ILoggedUser loggedUser ,
            IDashboardReadOnlyRepository dashboardRepository ,
            IExpensesReadOnlyRepository expensesRepository ,
            IFinancialGoalReadOnlyRepository goalsRepository ,
            IRedisCacheService cache ,
            IBrazilClock clock )
        {
            _loggedUser = loggedUser;
            _dashboardRepository = dashboardRepository;
            _expensesRepository = expensesRepository;
            _goalsRepository = goalsRepository;
            _cache = cache;
            _clock = clock;
        }

        public async Task<ResponseMonthForecastJson> Execute ( CancellationToken cancellationToken )
        {
            var today = _clock.Today;
            var user = await _loggedUser.Get();

            // One key per month so FinancialCacheInvalidator can drop it; a value from an earlier day is stale.
            var cacheKey = CacheKeys.MonthForecast(user.Id , today.Year , today.Month);

            var cached = await _cache.GetAsync<ResponseMonthForecastJson>(cacheKey);

            if ( cached != null && cached.Today == today.Day ) {
                return cached;
            }

            // DbContext is not thread-safe: queries run sequentially.
            var incomes = await _dashboardRepository.GetDailyIncomesByMonthAsync(user.Id , today.Year , today.Month , cancellationToken);
            var expenses = await _expensesRepository.GetByPeriodAsync(user.Id , today.Month , today.Year , cancellationToken);
            var goals = await _goalsRepository.GetByPeriodAsync(user.Id , today.Month , today.Year , cancellationToken);

            var forecast = MonthForecastCalculator.Calculate(
                today ,
                incomes.Sum(i => i.Amount) ,
                expenses.Select(e => new ForecastExpense(BrazilClock.ToBrasilia(e.ExpenseDate).Day , e.CategoryId , e.Amount)).ToList() ,
                goals.Select(g => new ForecastGoal(g.CategoryId , g.Category.Name , g.TargetAmount)).ToList());

            var response = ToResponse(forecast);

            await _cache.SetAsync(cacheKey , response , CacheExpiration.Dashboard);

            return response;
        }

        private static ResponseMonthForecastJson ToResponse ( Domain.Services.Forecast.MonthForecast forecast ) => new()
        {
            Year = forecast.Year ,
            Month = forecast.Month ,
            Today = forecast.Today ,
            DaysInMonth = forecast.DaysInMonth ,
            IsReliable = forecast.IsReliable ,
            Income = forecast.Income ,
            ExpenseToDate = forecast.ExpenseToDate ,
            ScheduledExpense = forecast.ScheduledExpense ,
            DailyExpenseRate = forecast.DailyExpenseRate ,
            ProjectedExpense = forecast.ProjectedExpense ,
            ProjectedBalance = forecast.ProjectedBalance ,
            Goals = forecast.Goals.Select(g => new ResponseGoalForecastJson
            {
                CategoryId = g.CategoryId ,
                CategoryName = g.CategoryName ,
                TargetAmount = g.TargetAmount ,
                CurrentAmount = g.CurrentAmount ,
                ProjectedAmount = g.ProjectedAmount ,
                AlreadyExceeded = g.AlreadyExceeded ,
                WillExceed = g.WillExceed ,
                EstimatedExceedDay = g.EstimatedExceedDay
            }).ToList()
        };
    }
}
