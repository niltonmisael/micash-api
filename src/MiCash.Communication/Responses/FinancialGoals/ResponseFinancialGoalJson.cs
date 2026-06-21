namespace MiCash.Communication.Responses.FinancialGoals
{
    public class ResponseFinancialGoalJson
    {
        public long Id { get; set; }

        public long CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public decimal TargetAmount { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }
    }
}
