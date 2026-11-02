namespace MiCash.Domain.Services.Cache.FinancialInvalidator
{
    public class FinancialCacheInvalidator : IFinancialCacheInvalidator
    {
        private readonly IRedisCacheService _redisCacheService;

        public FinancialCacheInvalidator (
            IRedisCacheService redisCacheService )
        {
            _redisCacheService = redisCacheService;
        }

        public async Task InvalidateMonthAsync (
            long userId ,
            int month ,
            int year )
        {
            await Task.WhenAll(
                InvalidateDashboardAsync(userId , month , year) ,
                InvalidateFinancialGoalsAsync(userId , month , year) ,
                InvalidateFinancialInsightsAsync(userId , month , year)
            );
        }

        public async Task InvalidateFinancialGoalAsync (
            long userId ,
            long financialGoalId )
        {
            await _redisCacheService.RemoveAsync(
                CacheKeys.FinancialGoalById(
                    userId ,
                    financialGoalId));
        }

        private Task InvalidateDashboardAsync (
            long userId ,
            int month ,
            int year )
        {
            return Task.WhenAll(
                _redisCacheService.RemoveAsync(
                    CacheKeys.MonthlySummary(
                        userId ,
                        year ,
                        month)) ,

                _redisCacheService.RemoveAsync(
                    CacheKeys.ExpensesByCategory(
                        userId ,
                        year ,
                        month)) ,

                _redisCacheService.RemoveAsync(
                    CacheKeys.DailySummary(
                        userId ,
                        year ,
                        month))
            );
        }

        private Task InvalidateFinancialGoalsAsync (
            long userId ,
            int month ,
            int year )
        {
            return Task.WhenAll(
                _redisCacheService.RemoveAsync(
                    CacheKeys.FinancialGoalsByPeriod(
                        userId ,
                        month ,
                        year)) ,

                _redisCacheService.RemoveAsync(
                    CacheKeys.FinancialGoalsProgress(
                        userId ,
                        month ,
                        year)) ,

                _redisCacheService.RemoveAsync(
                    CacheKeys.FinancialGoalsAlerts(
                        userId ,
                        month ,
                        year))
            );
        }

        private Task InvalidateFinancialInsightsAsync (
            long userId ,
            int month ,
            int year )
        {
            return _redisCacheService.RemoveAsync(
                CacheKeys.FinancialInsights(
                    userId ,
                    month ,
                    year));
        }

    }
}

