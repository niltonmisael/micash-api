using MiCash.Application.UseCases.Dashboards.ExpensesByCategory;
using MiCash.Communication.Responses.Dashboards;
using MiCash.Domain.Repositories.Dashboards;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Expirations;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.Dashboard.GetExpensesByCategory;

public class GetExpensesByCategoryUseCase
    : IGetExpensesByCategoryUseCase
{
    private readonly ILoggedUser _loggedUser;
    private readonly IDashboardReadOnlyRepository _repository;
    private readonly IRedisCacheService _cacheService;

    public GetExpensesByCategoryUseCase (
        ILoggedUser loggedUser ,
        IDashboardReadOnlyRepository repository,
        IRedisCacheService cacheService)
    {
        _loggedUser = loggedUser;
        _repository = repository;
        _cacheService = cacheService;
    }

    public async Task<List<ResponseExpenseCategorySummaryJson>> Execute (
        int year ,
        int month ,
        CancellationToken cancellationToken )
    {
        var user = await _loggedUser.Get();

        var cacheKey = CacheKeys.ExpensesByCategory(
            user.Id ,
            year ,
            month);

        var cachedData = await _cacheService.GetAsync<List<ResponseExpenseCategorySummaryJson>>(cacheKey);

        if( cachedData != null ) {
            return cachedData;
        }

        var categories = await _repository.GetExpensesByCategoryAsync(
            user.Id ,
            year ,
            month ,
            cancellationToken);

        var totalExpenses = categories.Sum(x => x.Amount);

        var result = categories
            .Select(x => new ResponseExpenseCategorySummaryJson
            {
                CategoryId = x.CategoryId ,
                CategoryName = x.CategoryName ,
                Amount = x.Amount,
                CategoryColor = x.CategoryColor ,
                Percentage = totalExpenses == 0
                ? 0
                : Math.Round(
                    ( x.Amount / totalExpenses ) * 100 ,
                    2)
            })
            .ToList();

        await _cacheService.SetAsync(
            cacheKey ,
            result ,
            CacheExpiration.Dashboard);

        return result;
    }
}