using MiCash.Application.UseCases.Dashboards.MonthlySummary;
using MiCash.Communication.Responses.Dashboards;
using MiCash.Domain.Repositories.Dashboards;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Expirations;
using MiCash.Domain.Services.LoggedUser;

public class GetMonthlySummaryUseCase
    : IGetMonthlySummaryUseCase
{
    private readonly ILoggedUser _loggedUser;
    private readonly IDashboardReadOnlyRepository _dashboardReadOnlyRepository;
    private readonly IRedisCacheService _redisCacheService;

    public GetMonthlySummaryUseCase (
        ILoggedUser loggedUser ,
        IDashboardReadOnlyRepository dashboardReadOnlyRepository,
        IRedisCacheService redisCacheService)
    {
        _loggedUser = loggedUser;
        _dashboardReadOnlyRepository = dashboardReadOnlyRepository;
        _redisCacheService = redisCacheService;
    }

    public async Task<ResponseMonthlySummaryJson> Execute (
        int year ,
        int month ,
        CancellationToken cancellationToken )
    {
        var user = await _loggedUser.Get();

        var cacheKey = CacheKeys.MonthlySummary(
            user.Id ,
            year ,
            month);

        var cached =
            await _redisCacheService.GetAsync<ResponseMonthlySummaryJson>(cacheKey);

        if(cached != null ) {
            return cached;
        }

        // DbContext is not thread-safe: queries must run sequentially.
        var totalIncome =
            await _dashboardReadOnlyRepository
            .GetTotalAmountIncomesByMonth(
                user.Id ,
                year ,
                month ,
                cancellationToken);

        var totalExpense =
            await _dashboardReadOnlyRepository
                .GetTotalAmountExpensesByMonth(
                    user.Id ,
                    year ,
                    month ,
                    cancellationToken);

        var summary = new ResponseMonthlySummaryJson
        {
            Year = year ,
            Month = month ,
            TotalIncome = totalIncome ,
            TotalExpense = totalExpense ,
            Balance = totalIncome - totalExpense
        };

        await _redisCacheService.SetAsync(
            cacheKey ,
            summary ,
            CacheExpiration.Dashboard);

        return summary;
    }
}