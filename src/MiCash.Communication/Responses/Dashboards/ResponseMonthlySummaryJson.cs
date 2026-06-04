namespace MiCash.Communication.Responses.Dashboards
{
    public class ResponseMonthlySummaryJson
    {
        public decimal TotalIncome { get; set; }

        public decimal TotalExpense { get; set; }

        public decimal Balance { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }
    }
}
