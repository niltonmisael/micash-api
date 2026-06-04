namespace MiCash.Communication.Responses.Dashboards
{
    public class ResponseExpenseCategorySummaryJson
    {
        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CategoryColor { get; set; } = string.Empty;
        public decimal Percentage { get; set; }
    }
}
