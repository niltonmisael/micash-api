namespace MiCash.Domain.Services.FinancialInsights
{
    public interface IFinancialInsightAnalyzer
    {
        IReadOnlyCollection<FinancialInsight> Analyze (
            FinancialAnalysisData data );
    }
}
