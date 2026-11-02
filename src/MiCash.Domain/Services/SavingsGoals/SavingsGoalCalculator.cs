namespace MiCash.Domain.Services.SavingsGoals
{
    public sealed record SavingsGoalSummary (
        decimal SavedAmount ,
        decimal RemainingAmount ,
        decimal Percentage ,
        bool IsCompleted ,
        decimal? MonthlyAmountNeeded );

    /// <summary>
    /// All derived numbers of a savings goal, so the app holds no business rules.
    /// </summary>
    public static class SavingsGoalCalculator
    {
        public static SavingsGoalSummary Calculate (
            decimal targetAmount ,
            decimal savedAmount ,
            DateOnly? deadline ,
            DateOnly today )
        {
            var remaining = Math.Max(targetAmount - savedAmount , 0);

            var percentage = targetAmount <= 0
                ? 0
                : Math.Round(savedAmount / targetAmount * 100 , 2 , MidpointRounding.AwayFromZero);

            var isCompleted = targetAmount > 0 && savedAmount >= targetAmount;

            decimal? monthly = null;

            if ( deadline is not null && !isCompleted ) {
                // Months left including the current one; an overdue deadline asks for everything now.
                var monthsLeft = Math.Max(
                    ( deadline.Value.Year * 12 + deadline.Value.Month ) - ( today.Year * 12 + today.Month ) + 1 ,
                    1);

                // Round UP to the cent: paying the rounded value every month must reach the target
                // (1000 / 3 = 333.34, not 333.33 which would end 1 cent short).
                monthly = Math.Ceiling(remaining / monthsLeft * 100) / 100;
            }

            return new SavingsGoalSummary(savedAmount , remaining , percentage , isCompleted , monthly);
        }
    }
}
