using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Incomes;
using MiCash.Domain.Services.Pagination;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    internal class IncomeRepository : IIncomesReadOnlyRepository, IIncomesWriteOnlyRepository, IIncomesUpdateOnlyRepository
    {
        private readonly MiCashDbContext _dbContext;

        public IncomeRepository (MiCashDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Add ( Income income , CancellationToken cancellationToken )
        {
            await _dbContext.Incomes.AddAsync(income , cancellationToken);
        }

        public async Task DeleteAsync ( long incomeId , CancellationToken cancellationToken )
        {
            await _dbContext.Incomes.Where(i => i.Id == incomeId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        public async Task<PagedResult<Income>> GetAllAsync ( User userLogged , int page , int pageSize , CancellationToken cancellationToken )
        {
            var query = _dbContext
                .Incomes
                .AsNoTracking()
                .Where(expense => expense.UserId == userLogged.Id);

            var totalRecords = await query
                .CountAsync(cancellationToken);

            var incomes = await query
                .OrderByDescending(expense => expense.ReceivedAt)
                .Skip(( page - 1 ) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Income>
            {
                Data = incomes ,
                TotalRecords = totalRecords
            };
        }

        public async Task<Income?> GetByIdAsync ( long userId , long incomeId , CancellationToken cancellationToken )
        {
            return await _dbContext.Incomes.FirstOrDefaultAsync(i => i.UserId == userId && i.Id == incomeId , cancellationToken);
        }

        public async Task<IReadOnlyCollection<Income>> GetByPeriodAsync ( long userId , int month , int year , CancellationToken cancellationToken )
        {
            var startDate = new DateOnly(year , month , 1);

            var endDate = startDate.AddMonths(1);

            return await _dbContext
                .Incomes
                .AsNoTracking()
                .Where(income =>
                    income.UserId == userId &&
                    income.ReceivedAt >= startDate &&
                    income.ReceivedAt < endDate)
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateAsync ( Income income , CancellationToken cancellationToken )
        {
            await _dbContext.Incomes.Where(i => i.Id == income.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.Title , income.Title)
                    .SetProperty(i => i.Description , income.Description)
                    .SetProperty(i => i.Amount , income.Amount)
                    .SetProperty(i => i.ReceivedAt , income.ReceivedAt)
                    .SetProperty(i => i.Category , income.Category)
                    .SetProperty(i => i.IsRecurring , income.IsRecurring)
                    .SetProperty(i => i.UpdatedAt , DateTime.UtcNow) , cancellationToken);
        }
    }
}
