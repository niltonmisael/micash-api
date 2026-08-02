using MiCash.Domain.Services.FinancialInsights;

namespace MiCash.Domain.Services.IA
{
    public interface IAIService
    {
        Task<AIResponse> GenerateResponseAsync (
            AIRequest request ,
            CancellationToken cancellationToken );
    }
}
