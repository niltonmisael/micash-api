namespace MiCash.Communication.Responses.Subscriptions
{
    /// <summary>What the user's plan allows, for the app to show limits and offers. null = unlimited.</summary>
    public class ResponsePlanLimitsJson
    {
        /// <summary>"Free" or "Premium".</summary>
        public string Plan { get; set; } = string.Empty;

        public bool IsPremium { get; set; }

        public int? MaxFinancialGoalsPerMonth { get; set; }

        public int? MaxSavingsGoals { get; set; }

        /// <summary>Previous months visible in dashboard/goals besides the current one; null = whole history.</summary>
        public int? HistoryMonths { get; set; }

        public bool FullInsights { get; set; }

        public bool GoalWarningAlerts { get; set; }

        public bool CanCopyFinancialGoals { get; set; }

        public bool MonthForecast { get; set; }

        public ResponseChatLimitJson Chat { get; set; } = new();
    }

    public class ResponseChatLimitJson
    {
        /// <summary>"month" (Free) or "day" (Premium).</summary>
        public string Period { get; set; } = string.Empty;

        public int Limit { get; set; }

        public int Used { get; set; }

        public int Remaining { get; set; }

        /// <summary>When the counter restarts (UTC; 00:00 in Brasília).</summary>
        public DateTime ResetsAt { get; set; }

        /// <summary>Extra anti-abuse limit (Premium); null when none.</summary>
        public int? PerMinute { get; set; }
    }
}
