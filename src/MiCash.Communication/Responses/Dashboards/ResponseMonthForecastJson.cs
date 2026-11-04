namespace MiCash.Communication.Responses.Dashboards
{
    /// <summary>Month-end forecast of the current month (Brasília calendar), at today's spending pace.</summary>
    public class ResponseMonthForecastJson
    {
        public int Year { get; set; }

        public int Month { get; set; }

        /// <summary>Day of the month the forecast was computed on (Brasília).</summary>
        public int Today { get; set; }

        public int DaysInMonth { get; set; }

        /// <summary>False with fewer than 5 days elapsed or fewer than 3 expenses until today: too little data.</summary>
        public bool IsReliable { get; set; }

        /// <summary>Income registered in the month (not projected).</summary>
        public decimal Income { get; set; }

        /// <summary>Expenses from day 1 to today.</summary>
        public decimal ExpenseToDate { get; set; }

        /// <summary>Expenses already registered for days after today; added to the forecast as they are.</summary>
        public decimal ScheduledExpense { get; set; }

        /// <summary>Average spending per day until today.</summary>
        public decimal DailyExpenseRate { get; set; }

        public decimal ProjectedExpense { get; set; }

        /// <summary>Income − ProjectedExpense: how much is left (or missing) at the end of the month.</summary>
        public decimal ProjectedBalance { get; set; }

        public List<ResponseGoalForecastJson> Goals { get; set; } = [];
    }

    public class ResponseGoalForecastJson
    {
        public long CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public decimal TargetAmount { get; set; }

        /// <summary>Spent in the category this month (same value as the goal progress).</summary>
        public decimal CurrentAmount { get; set; }

        public decimal ProjectedAmount { get; set; }

        public bool AlreadyExceeded { get; set; }

        public bool WillExceed { get; set; }

        /// <summary>Day of the month the goal is expected to be exceeded; null when it is not.</summary>
        public int? EstimatedExceedDay { get; set; }
    }
}
