using MiCash.Domain.Repositories.Dashboards;
using MiCash.Domain.Repositories.Dashboards.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    public class DashboardRepository : IDashboardReadOnlyRepository
    {
        private readonly MiCashDbContext _dbContext;

        public DashboardRepository ( MiCashDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<decimal> GetTotalAmountExpensesByMonth ( long id , int year , int month , CancellationToken cancellationToken )
        {
            return await _dbContext.Expenses
                .AsNoTracking()
                .Where(expense => expense.UserId == id && expense.ExpenseDate.Year == year && expense.ExpenseDate.Month == month)
                .SumAsync(expense => expense.Amount , cancellationToken);
        }

        public async Task<decimal> GetTotalAmountIncomesByMonth ( long id , int year , int month , CancellationToken cancellationToken )
        {
            return await _dbContext.Incomes
                .AsNoTracking()
                .Where(i => i.UserId == id && i.ReceivedAt.Year == year && i.ReceivedAt.Month == month)
                .SumAsync(i => i.Amount , cancellationToken);
        }

        public async Task<IEnumerable<ExpenseCategorySummary>> GetExpensesByCategoryAsync (
            long userId ,
            int year ,
            int month ,
            CancellationToken cancellationToken )
        {
            return await _dbContext.Expenses
                .AsNoTracking()
                .Where(expense =>
                    expense.UserId == userId &&
                    expense.ExpenseDate.Year == year &&
                    expense.ExpenseDate.Month == month)
                .GroupBy(expense => new {
                    expense.CategoryId ,
                    expense.Category.Name
                })
                .Select(group => new ExpenseCategorySummary
                {
                    CategoryId = group.Key.CategoryId ,
                    CategoryName = group.Key.Name ,
                    CategoryColor = _dbContext.Categories
                        .Where(c => c.Id == group.Key.CategoryId)
                        .Select(c => c.Color)
                        .FirstOrDefault() ?? string.Empty ,
                    Amount = group.Sum(expense => expense.Amount)
                })
                .OrderByDescending(x => x.Amount)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<LatestExpense>> GetLatestExpensesAsync ( long userId , int count , CancellationToken cancellationToken )
        {
            return await _dbContext.Expenses
                .AsNoTracking()
                .Where(expense => expense.UserId == userId)
                .OrderByDescending(expense => expense.ExpenseDate)
                .ThenByDescending(expense => expense.Id)
                .Take(count)
                .Select(expense => new LatestExpense
                {
                    Id = expense.Id ,
                    Title = expense.Title ,
                    Category = expense.Category.Name ,
                    Color = expense.Category.Color ,
                    Amount = expense.Amount ,
                    ExpenseDate = expense.ExpenseDate
                })
                .ToListAsync(cancellationToken);
        }
    }
}
