namespace MiCash.Domain.Repositories.Dashboards.DTOs
{
    public class IncomeCategorySummary
    {
        public int Category { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public decimal Percentage { get; set; }
    }
}
