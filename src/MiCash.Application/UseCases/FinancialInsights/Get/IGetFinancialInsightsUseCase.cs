using MiCash.Communication.Requests.FinancialInsights;
using MiCash.Communication.Responses.FinancialInsights;

namespace MiCash.Application.UseCases.FinancialInsights.Get
{
    public interface IGetFinancialInsightsUseCase
    {
        Task<ResponseFinancialInsightsJson> Execute (
            RequestFinancialInsightJson request,
            CancellationToken cancellationToken );
    }
}
