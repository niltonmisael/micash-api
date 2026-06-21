namespace MiCash.Communication.Responses.FinancialInsights
{
    public class ResponseFinancialInsightJson
    {
        public int Type { get; set; }

        public string TypeDescription { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}