namespace MiCash.Communication.Responses.FinancialInsights
{
    public class ResponseFinancialInsightsJson
    {
        public IReadOnlyList<ResponseFinancialInsightJson> Insights { get; set; } = [];

        public ResponseArtificialInsightJson? ArtificialInsight { get; set; }
    }
}
