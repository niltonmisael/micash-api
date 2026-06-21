using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Expenses;
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
        public async Task<List<Expense>> GetAll (User user , CancellationToken cancellationToken )
        {
            return await _dbContext.Expenses.AsNoTracking().Where(expense => expense.UserId == user.Id).ToListAsync(cancellationToken);
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

        public async Task<List<Expense>> FilterByMonth ( User user, DateOnly date, CancellationToken cancellationToken )
        {
            var startDate = new DateTime(date.Year , date.Month , 1 , 0 , 0 , 0 , DateTimeKind.Utc);
            var daysInMonth = DateTime.DaysInMonth(year: date.Year , month: date.Month);
            var endDate = new DateTime(date.Year , date.Month , daysInMonth , 23 , 59 , 59 , DateTimeKind.Utc);

            return await _dbContext
                .Expenses
                .AsNoTracking()
                .Where(expense => expense.UserId == user.Id && expense.ExpenseDate >= startDate && expense.ExpenseDate <= endDate)
                .OrderBy(expense => expense.ExpenseDate)
                .ThenBy(expense => expense.Title)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<Expense>> GetByPeriodAsync (
            long userId ,
            int month ,
            int year ,
            CancellationToken cancellationToken )
        {
            var startDate = new DateTime(year , month , 1);

            var endDate = startDate.AddMonths(1);

            return await _dbContext
                .Expenses
                .AsNoTracking()
                .Where(expense =>
                    expense.UserId == userId &&
                    expense.ExpenseDate >= startDate &&
                    expense.ExpenseDate < endDate)
                .ToListAsync(cancellationToken);
        }
    }
}
