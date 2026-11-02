namespace MiCash.Communication.Responses.Dashboards
{
    public class ResponseDailySummaryJson
    {
        public int Year { get; set; }

        public int Month { get; set; }

        /// <summary>Every day of the month (28 to 31 items), including days without entries (0).</summary>
        public List<ResponseDailySummaryItemJson> Days { get; set; } = [];
    }

    public class ResponseDailySummaryItemJson
    {
        public int Day { get; set; }

        public decimal Income { get; set; }

        public decimal Expense { get; set; }

        /// <summary>Income from day 1 to this day; on the last day it equals monthly-summary totalIncome.</summary>
        public decimal CumulativeIncome { get; set; }

        /// <summary>Expenses from day 1 to this day; on the last day it equals monthly-summary totalExpense.</summary>
        public decimal CumulativeExpense { get; set; }
    }
}
