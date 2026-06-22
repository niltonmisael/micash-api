namespace MiCash.Domain.Services.Cache.Expirations
{
    public static class CacheExpiration
    {
        public static readonly TimeSpan Dashboard =
            TimeSpan.FromMinutes(10);

        public static readonly TimeSpan FinancialGoals =
            TimeSpan.FromMinutes(15);

        public static readonly TimeSpan FinancialInsights =
            TimeSpan.FromMinutes(15);

        public static readonly TimeSpan Subscription =
            TimeSpan.FromMinutes(5);

        public static readonly TimeSpan Expense =
            TimeSpan.FromMinutes(15);

        public static readonly TimeSpan Income =
            TimeSpan.FromMinutes(15);

        public static readonly TimeSpan Category =
            TimeSpan.FromMinutes(15);

        public static readonly TimeSpan Plan =
            TimeSpan.FromDays(7);
    }
}
