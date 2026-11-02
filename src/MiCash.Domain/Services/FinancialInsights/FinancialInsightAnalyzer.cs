using MiCash.Domain.Enums;

namespace MiCash.Domain.Services.FinancialInsights
{
    public class FinancialInsightAnalyzer : IFinancialInsightAnalyzer
    {
        public IReadOnlyCollection<FinancialInsight> Analyze ( FinancialAnalysisData data )
        {
            var insights = new List<FinancialInsight>();

            AddExceededGoalsInsights(data , insights);

            AddHighestExpenseCategoryInsight(data , insights);

            AddNegativeBalanceInsight(data , insights);

            return insights;
        }

        private static void AddExceededGoalsInsights (
            FinancialAnalysisData data ,
            List<FinancialInsight> insights )
        {
            foreach ( var goal in data.FinancialGoals ) {
                var spentAmount = data.Expenses
                    .Where(expense =>
                        expense.CategoryId == goal.CategoryId)
                    .Sum(expense =>
                        expense.Amount);

                if ( spentAmount <= goal.TargetAmount ) {
                    continue;
                }

                var exceededAmount =
                    spentAmount - goal.TargetAmount;

                insights.Add(new FinancialInsight
                {
                    Type = FinancialInsightType.Warning ,

                    Title = "Meta ultrapassada" ,

                    Message =
                        $"Você ultrapassou sua meta de {goal.Category.Name} em R$ {exceededAmount:F2}."
                });
            }
        }

        private static void AddHighestExpenseCategoryInsight (
            FinancialAnalysisData data ,
            List<FinancialInsight> insights )
        {
            if ( !data.Expenses.Any() ) {
                return;
            }

            var totalExpenses =
                data.Expenses.Sum(x => x.Amount);

            if ( totalExpenses <= 0 ) {
                return;
            }

            // Group by id: with AsNoTracking each expense may carry its own Category instance.
            var highestCategory = data.Expenses
                .GroupBy(expense => expense.CategoryId)
                .Select(group => new {
                    CategoryName = group.First().Category?.Name ?? "Sem categoria" ,
                    Amount = group.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.Amount)
                .First();

            var percentage =
                ( highestCategory.Amount / totalExpenses ) * 100;

            insights.Add(new FinancialInsight
            {
                Type = FinancialInsightType.Information ,

                Title = "Maior gasto do mês" ,

                Message =
                    $"{highestCategory.CategoryName} representou {Math.Round(percentage , 2)}% das suas despesas neste período."
            });
        }

        private static void AddNegativeBalanceInsight (
            FinancialAnalysisData data ,
            List<FinancialInsight> insights )
        {
            var totalIncome =
                data.Incomes.Sum(income =>
                    income.Amount);

            var totalExpense =
                data.Expenses.Sum(expense =>
                    expense.Amount);

            if ( totalExpense <= totalIncome ) {
                return;
            }

            var difference =
                totalExpense - totalIncome;

            insights.Add(new FinancialInsight
            {
                Type = FinancialInsightType.Warning ,

                Title = "Gastos acima da renda" ,

                Message =
                    $"Você gastou R$ {difference:F2} a mais do que recebeu neste mês."
            });
        }
    }
}

