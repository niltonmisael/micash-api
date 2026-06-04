using MiCash.Communication.Enums;

namespace MiCash.Communication.Requests
{
    public class RequestExpenseJson
    {
        public string Title {  get; set; } = string.Empty;

        public string Description {  get; set; } = string.Empty;

        public DateTime ExpenseDate {  get; set; }

        public decimal Amount { get; set; }

        public PaymentType PaymentType { get; set; }

        public ExpenseStatus ExpenseStatus { get; set; }

        public long CategoryId { get; set; }

    }
}
