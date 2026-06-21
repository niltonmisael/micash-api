using MiCash.Domain.Entities;

namespace MiCash.Domain.Services.FinancialInsights
{
    public class FinancialAnalysisData
    {
        public IReadOnlyCollection<Expense> Expenses { get; set; } = [];

        public IReadOnlyCollection<Income> Incomes { get; set; } = [];

        public IReadOnlyCollection<FinancialGoal> FinancialGoals { get; set; } = [];
    }
}
