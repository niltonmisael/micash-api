namespace MiCash.Communication.Responses.Dashboards
{
    public class ResponseLatestExpenseJson
    {
        public long Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public DateTime ExpenseDate { get; set; }
    }
}
