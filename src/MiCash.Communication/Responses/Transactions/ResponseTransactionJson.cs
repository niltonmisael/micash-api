using MiCash.Communication.Enums;

namespace MiCash.Communication.Responses.Transactions
{
    /// <summary>One income or expense of the transactions list. Fields of the other kind come null.</summary>
    public class ResponseTransactionJson
    {
        public long Id { get; set; }

        public TransactionType Type { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>Always positive; the type says whether it adds or subtracts.</summary>
        public decimal Amount { get; set; }

        /// <summary>Day on the Brasília calendar (expense instants converted; income ReceivedAt as is).</summary>
        public DateOnly Date { get; set; }

        /// <summary>Expense: the stored UTC instant (keep it when editing).</summary>
        public DateTime? ExpenseDate { get; set; }

        public long? CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public string? CategoryColor { get; set; }

        public string? CategoryIcon { get; set; }

        public PaymentType? PaymentType { get; set; }

        public ExpenseStatus? ExpenseStatus { get; set; }

        public IncomeCategory? IncomeCategory { get; set; }

        public bool? IsRecurring { get; set; }
    }
}
