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
        public async Task Add ( Expense expense )
        {
             await _dbContext.Expenses.AddAsync(expense);

        }
        public async Task<List<Expense>> GetAll (User user )
        {
            return await _dbContext.Expenses.AsNoTracking().Where(expense => expense.UserId == user.Id).ToListAsync();
        }

        async Task<Expense?> IExpensesReadOnlyRepository.GetById ( User user, long id )
        {
            return await _dbContext.Expenses.FirstOrDefaultAsync(expense => expense.Id == id && expense.UserId == user.Id);
        }
        async Task<Expense?> IExpensesUpdateOnlyRepository.GetById ( User user, long id )
        {
            return await _dbContext.Expenses.FirstOrDefaultAsync(expense => expense.Id == id && expense.UserId == user.Id);
        }

        public async Task Delete (long id )
        {
            var result = await _dbContext.Expenses.FirstAsync(expense => expense.Id == id);

            _dbContext.Expenses.Remove(result);
            
        }

        public void Update ( Expense expense )
        {
            _dbContext.Expenses.Update(expense);
        }

        public async Task<List<Expense>> FilterByMonth ( DateOnly date )
        {
            var startDate = new DateTime(year: date.Year , month: date.Month , day: 1).Date;

            var daysInMonth = DateTime.DaysInMonth(year: date.Year , month: date.Month);
            var endDate = new DateTime(year: date.Year , month: date.Month , day: daysInMonth, hour: 23, minute: 59, second: 59);

            return await _dbContext
                .Expenses
                .AsNoTracking()
                .Where(expense => expense.Date >= startDate && expense.Date <= endDate)
                .OrderBy(expense => expense.Date)
                .ThenBy(expense => expense.Title)
                .ToListAsync();
        }
    }
}
