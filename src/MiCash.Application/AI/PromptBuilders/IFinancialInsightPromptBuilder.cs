using MiCash.Domain.Services.FinancialInsights;
using MiCash.Domain.Services.IA;

namespace MiCash.Application.AI.PromptBuilders
{
    public interface IFinancialInsightPromptBuilder
    {
        AIRequest BuildFinancialInsights ( FinancialAnalysisData analysisData );
    }
}
