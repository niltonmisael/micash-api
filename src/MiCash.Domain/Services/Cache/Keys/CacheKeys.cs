public static class CacheKeys
{
    public static string MonthlySummary (
        long userId ,
        int year ,
        int month )
        => $"dashboard:monthly-summary:{userId}:{year}:{month}";

    public static string DailySummary (
        long userId ,
        int year ,
        int month )
        => $"dashboard:daily-summary:{userId}:{year}:{month}";

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

    public static string FinancialInsights (
        long userId ,
        int month ,
        int year )
        => $"financial-insights:{userId}:{year}:{month}";
    
    public static string FinancialGoalsProgress (
        long userId ,
        int month ,
        int year )
        => $"financial-goals:progress:{userId}:{year}:{month}";

    public static string FinancialGoalsAlerts (
        long userId ,
        int month ,
        int year )
        => $"financial-goals-alerts:{userId}:{year}:{month}";

    public static string ExpenseById (
        long userId ,
        long expenseId )
        => $"expenses:{userId}:{expenseId}";

    public static string IncomeById (
        long userId ,
        long incomeId )
        => $"incomes:{userId}:{incomeId}";

    public static string Categories ( long userId )
    => $"categories:{userId}";

    public static string CategoryById ( long userId, long categoryId  )
    => $"categories:{userId}:{categoryId}";

    public static string Plans ()
    => "plans";

    public static string PlanById (long planId)
    => $"plans:{planId}";
}