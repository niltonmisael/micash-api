using MiCash.Communication.Enums;

namespace MiCash.Communication.Requests.Incomes
{
    public class RequestIncomeJson
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public DateOnly ReceivedAt { get; set; }
        public IncomeCategory IncomeCategory { get; set; }
        public bool IsRecurring { get; set; }

    }
}
