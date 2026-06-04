namespace MiCash.Domain.Repositories.Dashboards.DTOs
{
    public class LatestExpense
    {
        public long Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public DateTime ExpenseDate { get; set; }
    }
}
