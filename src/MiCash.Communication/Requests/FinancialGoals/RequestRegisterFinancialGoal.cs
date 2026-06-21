namespace MiCash.Communication.Requests.FinancialGoals
{
    public class RequestRegisterFinancialGoal
    {
        public long CategoryId { get; set; }

        public decimal TargetAmount { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }
    }
}
