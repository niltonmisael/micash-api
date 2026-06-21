namespace MiCash.Communication.Responses.FinancialGoals
{
    public class ResponseFinancialGoalAlertJson
    {
        public long CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public decimal TargetAmount { get; set; }

        public decimal CurrentAmount { get; set; }

        public decimal DifferenceAmount { get; set; }

        public int Status { get; set; }

        public string StatusDescription { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}