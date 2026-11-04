namespace MiCash.Domain.Services.Plans
{
    /// <summary>
    /// What each plan allows. null = unlimited. New plans (e.g. Family) are a new instance here.
    /// Rule of thumb: never limit data entry (expenses, incomes, categories), only depth and intelligence.
    /// </summary>
    public sealed record PlanLimits
    {
        public string Name { get; init; } = string.Empty;

        public bool IsPremium { get; init; }

        /// <summary>Spending goals per month.</summary>
        public int? MaxFinancialGoalsPerMonth { get; init; }

        public int? MaxSavingsGoals { get; init; }

        /// <summary>
        /// Previous months visible in the dashboard and goal screens, besides the current one (and future ones).
        /// 3 = current month + the 3 previous.
        /// </summary>
        public int? HistoryMonths { get; init; }

        public int? ChatMessagesPerMonth { get; init; }

        public int? ChatMessagesPerDay { get; init; }

        public int? ChatMessagesPerMinute { get; init; }

        /// <summary>False: insights come as a preview (first insight only, no AI analysis).</summary>
        public bool FullInsights { get; init; }

        /// <summary>False: goal alerts only when exceeded (100%), not the 80% warning.</summary>
        public bool GoalWarningAlerts { get; init; }

        public bool CanCopyFinancialGoals { get; init; }

        public bool MonthForecast { get; init; }

        public static readonly PlanLimits Free = new()
        {
            Name = "Free" ,
            IsPremium = false ,
            MaxFinancialGoalsPerMonth = 3 ,
            MaxSavingsGoals = 1 ,
            HistoryMonths = 3 ,
            ChatMessagesPerMonth = 5 ,
            FullInsights = false ,
            GoalWarningAlerts = false ,
            CanCopyFinancialGoals = false ,
            MonthForecast = false
        };

        public static readonly PlanLimits Premium = new()
        {
            Name = "Premium" ,
            IsPremium = true ,
            ChatMessagesPerDay = 30 ,
            ChatMessagesPerMinute = 5 ,
            FullInsights = true ,
            GoalWarningAlerts = true ,
            CanCopyFinancialGoals = true ,
            MonthForecast = true
        };

        /// <summary>Whether the month can be viewed today (Brasília calendar). Future months are always allowed.</summary>
        public bool AllowsMonth ( DateOnly today , int year , int month )
        {
            if ( HistoryMonths is null ) {
                return true;
            }

            return year * 12 + month >= today.Year * 12 + today.Month - HistoryMonths.Value;
        }
    }
}
