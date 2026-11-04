namespace MiCash.Communication.Responses.FinancialInsights
{
    public class ResponseFinancialInsightsJson
    {
        public IReadOnlyList<ResponseFinancialInsightJson> Insights { get; set; } = [];

        public ResponseArtificialInsightJson? ArtificialInsight { get; set; }

        /// <summary>True on the Free plan: only the first insight is sent and there is no AI analysis.</summary>
        public bool IsPreview { get; set; }

        /// <summary>How many insights are hidden by the preview (including the AI analysis); 0 on Premium.</summary>
        public int LockedInsightsCount { get; set; }
    }
}
