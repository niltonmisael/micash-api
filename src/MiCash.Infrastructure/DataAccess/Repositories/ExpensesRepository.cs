using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Services.Clock;
using MiCash.Domain.Services.Pagination;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    internal class ExpensesRepository : IExpensesWriteOnlyRepository, IExpensesReadOnlyRepository, IExpensesUpdateOnlyRepository
    {
        private readonly MiCashDbContext _dbContext;

        public ExpensesRepository (MiCashDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task Add ( Expense expense, CancellationToken cancellationToken )
        {
             await _dbContext.Expenses.AddAsync(expense, cancellationToken);

        }
        
        async Task<Expense?> IExpensesReadOnlyRepository.GetById ( User user, long id, CancellationToken cancellationToken )
        {
            return await _dbContext.Expenses
                .AsNoTracking()
                .FirstOrDefaultAsync(expense => expense.Id == id && expense.UserId == user.Id, cancellationToken);
        }
        async Task<Expense?> IExpensesUpdateOnlyRepository.GetById ( User user, long id, CancellationToken cancellationToken )
        {
            return await _dbContext.Expenses
                .FirstOrDefaultAsync(expense => expense.Id == id && expense.UserId == user.Id, cancellationToken);
        }

        public async Task Delete (long id, CancellationToken cancellationToken )
        {
            var result = await _dbContext.Expenses.FirstAsync(expense => expense.Id == id, cancellationToken);

            _dbContext.Expenses.Remove(result);
            
        }

        public void Update ( Expense expense, CancellationToken cancellationToken)
        {
            _dbContext.Expenses.Update(expense);
        }

        public async Task<IReadOnlyCollection<Expense>> GetByPeriodAsync (
            long userId ,
            int month ,
            int year ,
            CancellationToken cancellationToken )
        {
            // Month of the Brasília calendar (ExpenseDate is a UTC instant).
            var (startDate, endDate) = BrazilClock.MonthRangeUtc(year , month);

            return await _dbContext
                .Expenses
                .AsNoTracking()
                .Include(expense => expense.Category)
                .Where(expense =>
                    expense.UserId == userId &&
                    expense.ExpenseDate >= startDate &&
                    expense.ExpenseDate < endDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<PagedResult<Expense>> GetAllAsync (
            long userId ,
            int page ,
            int pageSize ,
            CancellationToken cancellationToken )
        {
            var query = _dbContext
                .Expenses
                .AsNoTracking()
                .Where(expense => expense.UserId == userId);

            var totalRecords = await query
                .CountAsync(cancellationToken);

            var expenses = await query
                .OrderByDescending(expense => expense.ExpenseDate)
                .Skip(( page - 1 ) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Expense>
            {
                Data = expenses ,
                TotalRecords = totalRecords
            };
        }
    }
}
