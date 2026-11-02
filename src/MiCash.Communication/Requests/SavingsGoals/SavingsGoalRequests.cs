namespace MiCash.Communication.Requests.SavingsGoals
{
    public class RequestRegisterSavingsGoalJson
    {
        public string Name { get; set; } = string.Empty;

        public decimal TargetAmount { get; set; }

        /// <summary>Optional. The app picks month/year and sends the last day of the month (2026-12-31).</summary>
        public DateOnly? Deadline { get; set; }

        /// <summary>"Já guardado (opcional)": becomes an initial deposit so it shows in the history.</summary>
        public decimal? InitialAmount { get; set; }

        public string Icon { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;
    }

    public class RequestUpdateSavingsGoalJson
    {
        public string Name { get; set; } = string.Empty;

        public decimal TargetAmount { get; set; }

        public DateOnly? Deadline { get; set; }

        public string Icon { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;
    }

    public class RequestSavingsMovementJson
    {
        public decimal Amount { get; set; }

        public DateOnly Date { get; set; }
    }
}
