using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.FinancialGoals;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    public sealed class FinancialGoalRepository : 
        IFinancialGoalReadOnlyRepository, 
        IFinancialGoalWriteOnlyRepository, 
        IFinancialGoalUpdateOnlyRepository
    {
        private readonly MiCashDbContext _dbContext;

        public FinancialGoalRepository (
            MiCashDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync ( FinancialGoal goal , CancellationToken cancellationToken )
        {
            await _dbContext.FinancialGoals.AddAsync ( goal , cancellationToken );
        }

        public async Task AddRangeAsync ( IEnumerable<FinancialGoal> goals , CancellationToken cancellationToken )
        {
            await _dbContext.FinancialGoals.AddRangeAsync ( goals , cancellationToken );
        }

        public void Delete ( FinancialGoal goal )
        {
            _dbContext.FinancialGoals.Remove(goal);
        }

        public Task<bool> ExistsAsync ( long userId , long categoryId , int month , int year , CancellationToken cancellationToken )
        {
            throw new NotImplementedException();
        }

        public Task<FinancialGoal?> GetByIdAsync ( long id , CancellationToken cancellationToken )
        {
            throw new NotImplementedException();
        }

        public Task<List<FinancialGoal>> GetByPeriodAndCategoriesAsync ( long userId , int month , int year , CancellationToken cancellationToken )
        {
            throw new NotImplementedException();
        }

        public Task<List<FinancialGoal>> GetByPeriodAsync ( long userId , int month , int year , CancellationToken cancellationToken )
        {
            throw new NotImplementedException();
        }

        public async Task<IReadOnlyCollection<FinancialGoal>> GetHistoryByUserIdAsync ( long userId , CancellationToken cancellationToken )
        {
            return await _dbContext
                .FinancialGoals
                .AsNoTracking()
                .Include(x => x.Category)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyCollection<FinancialGoal> , int)> GetHistoryByUserIdPagedAsync (
            long userId ,
            int page ,
            int pageSize ,
            CancellationToken cancellationToken )
        {
            var query = _dbContext
                .FinancialGoals
                .AsNoTracking()
                .Include(x => x.Category)
                .Where(x => x.UserId == userId);

            var totalRecords =
                await query.CountAsync(cancellationToken);

            var goals =
                await query
                    .OrderByDescending(x => x.Year)
                    .ThenByDescending(x => x.Month)
                    .Skip(( page - 1 ) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

            return (goals , totalRecords);
        }

        public void Update ( FinancialGoal goal )
        {
            _dbContext.FinancialGoals.Update (goal);
        }
    }
}
