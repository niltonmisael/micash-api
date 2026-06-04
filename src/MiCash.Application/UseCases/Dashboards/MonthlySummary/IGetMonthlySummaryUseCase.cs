using MiCash.Communication.Responses.Dashboards;

namespace MiCash.Application.UseCases.Dashboards.MonthlySummary
{
    public interface IGetMonthlySummaryUseCase
    {
        Task<ResponseMonthlySummaryJson> Execute ( int year , int month , CancellationToken cancellationToken );
    }
}
