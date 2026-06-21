public static class CacheKeys
{
    public static string MonthlySummary (
        long userId ,
        int year ,
        int month )
        => $"dashboard:monthly-summary:{userId}:{year}:{month}";

    public static string ExpensesByCategory (
        long userId ,
        int year ,
        int month )
        => $"dashboard:expenses-by-category:{userId}:{year}:{month}";

    public static string SubscriptionHistory ( long userId )
        => $"subscription:history:{userId}";

    public static string FinancialGoalById ( long userId , long financialGoalId )
        => $"financial-goal:{userId}:{financialGoalId}";

    public static string FinancialGoalsByPeriod (
        long userId ,
        int month ,
        int year )
        => $"financial-goals:by-period:{userId}:{year}:{month}";

    public static string FinancialGoalsHistory (
        long userId ,
        int page ,
        int pageSize )
        => $"financial-goals-history:{userId}:{page}:{pageSize}";
}