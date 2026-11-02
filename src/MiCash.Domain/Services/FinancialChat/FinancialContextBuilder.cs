using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Dashboards.DTOs;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Repositories.Incomes;

namespace MiCash.Domain.Services.FinancialChat
{
    public class FinancialContextBuilder : IFinancialContextBuilder
    {
        private readonly IExpensesReadOnlyRepository _expensesRepository;

        private readonly IIncomesReadOnlyRepository _incomesRepository;

        private readonly IFinancialGoalReadOnlyRepository _goalRepository;

        public FinancialContextBuilder (
            IExpensesReadOnlyRepository expensesRepository ,
            IIncomesReadOnlyRepository incomesRepository ,
            IFinancialGoalReadOnlyRepository goalRepository )
        {
            _expensesRepository = expensesRepository;
            _incomesRepository = incomesRepository;
            _goalRepository = goalRepository;
        }

        public async Task<FinancialChatContext> BuildAsync (
            User user ,
            string userMessage ,
            IReadOnlyList<ConversationMessage> history ,
            CancellationToken cancellationToken )
        {
            var today = DateTime.UtcNow;

            // DbContext is not thread-safe: queries must run sequentially.
            var incomes =
                await _incomesRepository.GetByPeriodAsync(
                    user.Id ,
                    today.Month ,
                    today.Year ,
                    cancellationToken);

            var expenses =
                await _expensesRepository.GetByPeriodAsync(
                    user.Id ,
                    today.Month ,
                    today.Year ,
                    cancellationToken);

            var goals =
                await _goalRepository.GetByPeriodAsync(
                    user.Id ,
                    today.Month ,
                    today.Year ,
                    cancellationToken);

            var totalIncome =
                incomes.Sum(x => x.Amount);

            var totalExpense =
                expenses.Sum(x => x.Amount);

            return new FinancialChatContext
            {
                UserName = user.Name ,

                UserMessage = userMessage ,

                Month = today.Month ,

                Year = today.Year ,

                TotalIncome = totalIncome ,

                TotalExpense = totalExpense ,

                Balance = totalIncome - totalExpense ,

                FinancialGoals = goals ,

                ExpensesByCategory =
                    BuildExpenseCategories(
                        expenses.ToList() ,
                        totalExpense) ,

                IncomesByCategory =
                    BuildIncomeCategories(
                        incomes.ToList() ,
                        totalIncome) ,

                ConversationHistory = history
                
            };
        }

        private static IReadOnlyList<ExpenseCategorySummary> BuildExpenseCategories (
            IReadOnlyList<Expense> expenses ,
            decimal totalExpense )
        {
            return expenses
                .GroupBy(expense => new {
                    expense.CategoryId ,
                    expense.Category.Name
                })
                .Select(group => new ExpenseCategorySummary
                {
                    CategoryId = group.Key.CategoryId ,

                    CategoryName = group.Key.Name ,

                    Amount = group.Sum(expense => expense.Amount) ,

                    Percentage = totalExpense == 0
                        ? 0
                        : Math.Round(
                            group.Sum(expense => expense.Amount) /
                            totalExpense * 100 ,
                            2)
                })
                .OrderByDescending(summary => summary.Amount)
                .ToList();
        }

        private static IReadOnlyList<IncomeCategorySummary> BuildIncomeCategories (
            IReadOnlyList<Income> incomes ,
            decimal totalIncome )
        {
            return incomes
                .GroupBy(income => income.Category)
                .Select(group => new IncomeCategorySummary
                {
                    Category = ( int ) group.Key ,

                    CategoryName = group.Key.ToString() ,

                    Amount = group.Sum(income => income.Amount) ,

                    Percentage = totalIncome == 0
                        ? 0
                        : Math.Round(
                            group.Sum(income => income.Amount) /
                            totalIncome * 100 ,
                            2)
                })
                .OrderByDescending(summary => summary.Amount)
                .ToList();
        }
    }
}
