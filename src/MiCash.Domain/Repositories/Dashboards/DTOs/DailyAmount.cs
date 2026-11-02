namespace MiCash.Domain.Repositories.Dashboards.DTOs
{
    /// <summary>Sum of the entries of one day of the month (days without entries are not returned).</summary>
    public class DailyAmount
    {
        public int Day { get; set; }

        public decimal Amount { get; set; }
    }
}
