using MiCash.Communication.Enums;

namespace MiCash.Communication.Responses.Incomes
{
    public class ResponseIncomeJson
    {
        public long Id { get; private set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public DateOnly ReceivedAt { get; set; }
        public IncomeCategory Category { get; set; }
    }
}
