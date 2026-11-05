using MiCash.Communication.Enums;

namespace MiCash.Communication.Requests.Incomes
{
    public class ResponseIncomeShortJson
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public DateOnly ReceivedAt { get; set; }
        public IncomeCategory Category { get; set; }
        public bool IsRecurring { get; set; }
    }
}
