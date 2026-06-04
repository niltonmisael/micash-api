using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Incomes;
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

        public async Task<IEnumerable<Income>> GetAllAsync ( User userLogged , CancellationToken cancellationToken )
        {
            return await _dbContext.Incomes.Where(i => i.UserId == userLogged.Id).ToListAsync(cancellationToken);
        }

        public async Task<Income?> GetByIdAsync ( long userId , long incomeId , CancellationToken cancellationToken )
        {
            return await _dbContext.Incomes.FirstOrDefaultAsync(i => i.UserId == userId && i.Id == incomeId , cancellationToken);
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
