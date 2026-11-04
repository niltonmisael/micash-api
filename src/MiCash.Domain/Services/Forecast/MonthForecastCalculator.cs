namespace MiCash.Domain.Services.Forecast
{
    /// <summary>One expense of the month already registered (Day = day of the month, same rule as daily-summary).</summary>
    public sealed record ForecastExpense ( int Day , long CategoryId , decimal Amount );

    /// <summary>A spending goal of the month for one category.</summary>
    public sealed record ForecastGoal ( long CategoryId , string CategoryName , decimal TargetAmount );

    public sealed class MonthForecast
    {
        public int Year { get; init; }
        public int Month { get; init; }
        public int Today { get; init; }
        public int DaysInMonth { get; init; }
        public bool IsReliable { get; init; }
        public decimal Income { get; init; }
        public decimal ExpenseToDate { get; init; }
        public decimal ScheduledExpense { get; init; }
        public decimal DailyExpenseRate { get; init; }
        public decimal ProjectedExpense { get; init; }
        public decimal ProjectedBalance { get; init; }
        public List<GoalForecast> Goals { get; init; } = [];
    }

    public sealed class GoalForecast
    {
        public long CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public decimal TargetAmount { get; init; }
        public decimal CurrentAmount { get; init; }
        public decimal ProjectedAmount { get; init; }
        public bool AlreadyExceeded { get; init; }
        public bool WillExceed { get; init; }
        public int? EstimatedExceedDay { get; init; }
    }

    /// <summary>
    /// Linear month-end forecast: the average daily spending up to today continues until the end of the month.
    /// Expenses already registered for days after today (bills scheduled ahead) are added as they are, not
    /// extrapolated. Income is never projected (salary arrives in one go): only what is registered counts.
    /// </summary>
    public static class MonthForecastCalculator
    {
        public const int MIN_DAYS_ELAPSED = 5;
        public const int MIN_EXPENSES = 3;

        public static MonthForecast Calculate (
            DateOnly today ,
            decimal income ,
            IReadOnlyCollection<ForecastExpense> expenses ,
            IReadOnlyCollection<ForecastGoal> goals )
        {
            var daysInMonth = DateTime.DaysInMonth(today.Year , today.Month);
            var elapsed = today.Day;
            var remaining = daysInMonth - elapsed;

            var past = expenses.Where(e => e.Day <= elapsed).ToList();
            var expenseToDate = past.Sum(e => e.Amount);
            var scheduled = expenses.Where(e => e.Day > elapsed).Sum(e => e.Amount);
            var rate = expenseToDate / elapsed;
            var projectedExpense = expenseToDate + rate * remaining + scheduled;

            return new MonthForecast
            {
                Year = today.Year ,
                Month = today.Month ,
                Today = elapsed ,
                DaysInMonth = daysInMonth ,
                IsReliable = elapsed >= MIN_DAYS_ELAPSED && past.Count >= MIN_EXPENSES ,
                Income = income ,
                ExpenseToDate = expenseToDate ,
                ScheduledExpense = scheduled ,
                DailyExpenseRate = Round(rate) ,
                ProjectedExpense = Round(projectedExpense) ,
                ProjectedBalance = Round(income - projectedExpense) ,
                Goals = goals.Select(goal => ForGoal(goal , expenses , elapsed , daysInMonth)).ToList()
            };
        }

        private static GoalForecast ForGoal ( ForecastGoal goal , IReadOnlyCollection<ForecastExpense> expenses , int elapsed , int daysInMonth )
        {
            var category = expenses.Where(e => e.CategoryId == goal.CategoryId).ToList();
            var toDate = category.Where(e => e.Day <= elapsed).Sum(e => e.Amount);
            var rate = toDate / elapsed;

            // Spent by the end of day d: what happened until today, the pace after it and what is scheduled until d.
            decimal SpentBy ( int day ) =>
                toDate + rate * ( day - elapsed ) + category.Where(e => e.Day > elapsed && e.Day <= day).Sum(e => e.Amount);

            var current = category.Sum(e => e.Amount);
            var projected = SpentBy(daysInMonth);

            // Same threshold as the goal progress (Exceeded at 100%).
            var alreadyExceeded = current >= goal.TargetAmount;
            var willExceed = !alreadyExceeded && projected > goal.TargetAmount;

            int? exceedDay = null;
            if ( willExceed ) {
                exceedDay = Enumerable.Range(elapsed + 1 , daysInMonth - elapsed).First(day => SpentBy(day) > goal.TargetAmount);
            }

            return new GoalForecast
            {
                CategoryId = goal.CategoryId ,
                CategoryName = goal.CategoryName ,
                TargetAmount = goal.TargetAmount ,
                CurrentAmount = current ,
                ProjectedAmount = Round(projected) ,
                AlreadyExceeded = alreadyExceeded ,
                WillExceed = willExceed ,
                EstimatedExceedDay = exceedDay
            };
        }

        private static decimal Round ( decimal value ) => Math.Round(value , 2 , MidpointRounding.AwayFromZero);
    }
}
