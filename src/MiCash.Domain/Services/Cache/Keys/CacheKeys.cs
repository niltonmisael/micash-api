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
}