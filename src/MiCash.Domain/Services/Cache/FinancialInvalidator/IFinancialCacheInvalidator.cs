namespace MiCash.Domain.Services.Cache.FinancialInvalidator
{
    public interface IFinancialCacheInvalidator
    {
        Task InvalidateMonthAsync (
            long userId ,
            int month ,
            int year );

        Task InvalidateFinancialGoalAsync (
            long userId ,
            long financialGoalId );

    }
}
