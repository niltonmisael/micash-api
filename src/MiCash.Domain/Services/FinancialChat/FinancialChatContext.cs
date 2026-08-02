using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Dashboards.DTOs;

namespace MiCash.Domain.Services.FinancialChat
{
    public class FinancialChatContext
    {
        public string UserName { get; set; } = string.Empty;

        public string UserMessage { get; set; } = string.Empty;

        public int Month { get; set; }

        public int Year { get; set; }

        public decimal TotalIncome { get; set; }

        public decimal TotalExpense { get; set; }

        public decimal Balance { get; set; }

        public IReadOnlyList<FinancialGoal> FinancialGoals { get; set; } = [];

        public IReadOnlyList<ExpenseCategorySummary> ExpensesByCategory { get; set; } = [];

        public IReadOnlyList<IncomeCategorySummary> IncomesByCategory { get; set; } = [];
        public IReadOnlyList<ConversationMessage> ConversationHistory { get; internal set; } = [];
    }
}
