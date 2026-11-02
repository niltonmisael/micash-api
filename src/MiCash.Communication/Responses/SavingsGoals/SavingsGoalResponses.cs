namespace MiCash.Communication.Responses.SavingsGoals
{
    public class ResponseRegisteredSavingsGoalJson
    {
        public long Id { get; set; }
    }

    public class ResponseRegisteredSavingsMovementJson
    {
        public long Id { get; set; }
    }

    public class ResponseSavingsGoalsJson
    {
        /// <summary>In progress first (by deadline, no deadline last), then completed.</summary>
        public List<ResponseShortSavingsGoalJson> SavingsGoals { get; set; } = [];
    }

    public class ResponseShortSavingsGoalJson
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public decimal TargetAmount { get; set; }

        public decimal SavedAmount { get; set; }

        /// <summary>May exceed 100; the app caps the bar.</summary>
        public decimal Percentage { get; set; }

        /// <summary>null = no deadline.</summary>
        public DateOnly? Deadline { get; set; }

        public bool IsCompleted { get; set; }
    }

    public class ResponseSavingsGoalJson : ResponseShortSavingsGoalJson
    {
        public decimal RemainingAmount { get; set; }

        /// <summary>null when there is no deadline or the goal is completed.</summary>
        public decimal? MonthlyAmountNeeded { get; set; }

        /// <summary>Newest first.</summary>
        public List<ResponseSavingsMovementJson> Movements { get; set; } = [];
    }

    public class ResponseSavingsMovementJson
    {
        public long Id { get; set; }

        /// <summary>0 = deposit, 1 = withdrawal.</summary>
        public int Type { get; set; }

        public decimal Amount { get; set; }

        public DateOnly Date { get; set; }
    }
}
