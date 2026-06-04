namespace MiCash.Domain.Repositories.Dashboards.DTOs
{
    public class ExpenseCategorySummary
    {
        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
