using MiCash.Communication.Responses.Dashboards;
using MiCash.Domain.Repositories.Dashboards;
using MiCash.Domain.Repositories.Dashboards.DTOs;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Expirations;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.Dashboards.DailySummary
{
    public interface IGetDailySummaryUseCase
    {
        Task<ResponseDailySummaryJson> Execute ( int year , int month , CancellationToken cancellationToken );
    }

    /// <summary>
    /// Income and expenses per day of the month (Dashboard chart), with the running totals computed here so the
    /// app sums nothing. Uses the same entries and month rule as monthly-summary: the last day's cumulative
    /// values equal its totals.
    /// </summary>
    public sealed class GetDailySummaryUseCase : IGetDailySummaryUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IDashboardReadOnlyRepository _repository;
        private readonly IRedisCacheService _cache;

        public GetDailySummaryUseCase ( ILoggedUser loggedUser , IDashboardReadOnlyRepository repository , IRedisCacheService cache )
        {
            _loggedUser = loggedUser;
            _repository = repository;
            _cache = cache;
        }

        public async Task<ResponseDailySummaryJson> Execute ( int year , int month , CancellationToken cancellationToken )
        {
            PeriodValidator.Validate(month , year);

            var user = await _loggedUser.Get();

            var cacheKey = CacheKeys.DailySummary(user.Id , year , month);

            var cached = await _cache.GetAsync<ResponseDailySummaryJson>(cacheKey);

            if ( cached != null ) {
                return cached;
            }

            // DbContext is not thread-safe: queries run sequentially.
            var incomes = await _repository.GetDailyIncomesByMonthAsync(user.Id , year , month , cancellationToken);
            var expenses = await _repository.GetDailyExpensesByMonthAsync(user.Id , year , month , cancellationToken);

            var summary = Build(year , month , incomes , expenses);

            await _cache.SetAsync(cacheKey , summary , CacheExpiration.Dashboard);

            return summary;
        }

        public static ResponseDailySummaryJson Build ( int year , int month , IEnumerable<DailyAmount> incomes , IEnumerable<DailyAmount> expenses )
        {
            var incomeByDay = incomes.GroupBy(x => x.Day).ToDictionary(g => g.Key , g => g.Sum(x => x.Amount));
            var expenseByDay = expenses.GroupBy(x => x.Day).ToDictionary(g => g.Key , g => g.Sum(x => x.Amount));

            var summary = new ResponseDailySummaryJson { Year = year , Month = month };

            decimal cumulativeIncome = 0;
            decimal cumulativeExpense = 0;

            for ( var day = 1; day <= DateTime.DaysInMonth(year , month); day++ ) {
                var income = incomeByDay.GetValueOrDefault(day);
                var expense = expenseByDay.GetValueOrDefault(day);

                cumulativeIncome += income;
                cumulativeExpense += expense;

                summary.Days.Add(new ResponseDailySummaryItemJson
                {
                    Day = day ,
                    Income = income ,
                    Expense = expense ,
                    CumulativeIncome = cumulativeIncome ,
                    CumulativeExpense = cumulativeExpense
                });
            }

            return summary;
        }
    }
}
