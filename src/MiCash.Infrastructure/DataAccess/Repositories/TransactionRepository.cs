using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Transactions;
using MiCash.Domain.Services.Clock;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    /// <summary>
    /// Reads one month of entries for the transactions list. Same month rules as DashboardRepository, so the
    /// list adds up to the monthly-summary totals.
    /// </summary>
    public class TransactionRepository : ITransactionsReadOnlyRepository
    {
        private readonly MiCashDbContext _dbContext;

        public TransactionRepository ( MiCashDbContext dbContext )
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<Expense>> GetExpensesOfMonthAsync ( long userId , int year , int month , CancellationToken cancellationToken )
        {
            var (start, end) = BrazilClock.MonthRangeUtc(year , month);

            return await _dbContext.Expenses
                .AsNoTracking()
                .Include(expense => expense.Category)
                .Where(expense => expense.UserId == userId && expense.ExpenseDate >= start && expense.ExpenseDate < end)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Income>> GetIncomesOfMonthAsync ( long userId , int year , int month , CancellationToken cancellationToken )
        {
            return await _dbContext.Incomes
                .AsNoTracking()
                .Where(income => income.UserId == userId && income.ReceivedAt.Year == year && income.ReceivedAt.Month == month)
                .ToListAsync(cancellationToken);
        }
    }
}
