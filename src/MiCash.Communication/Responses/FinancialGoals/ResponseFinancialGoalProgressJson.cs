namespace MiCash.Communication.Responses.FinancialGoals
{
    public class ResponseFinancialGoalProgressJson
    {
        public long CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public decimal TargetAmount { get; set; }

        public decimal CurrentAmount { get; set; }

        public decimal RemainingAmount { get; set; }

        public decimal PercentageUsed { get; set; }

        public int Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
    }
}
