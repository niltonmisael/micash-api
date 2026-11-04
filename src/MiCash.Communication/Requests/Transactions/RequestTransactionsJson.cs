using MiCash.Communication.Enums;

namespace MiCash.Communication.Requests.Transactions
{
    /// <summary>
    /// Query of GET api/transactions: incomes and expenses of one month (Brasília calendar), newest first.
    /// </summary>
    public class RequestTransactionsJson
    {
        public int Year { get; set; }

        public int Month { get; set; }

        /// <summary>Only incomes or only expenses; null = both.</summary>
        public TransactionType? Type { get; set; }

        /// <summary>Expense category of the user. Setting it lists expenses only.</summary>
        public long? CategoryId { get; set; }

        /// <summary>Income category. Setting it lists incomes only.</summary>
        public IncomeCategory? IncomeCategory { get; set; }

        /// <summary>Part of the title, ignoring case and accents.</summary>
        public string? Search { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}
