using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.SavingsGoals;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    public sealed class SavingsGoalRepository :
        ISavingsGoalReadOnlyRepository,
        ISavingsGoalWriteOnlyRepository,
        ISavingsGoalUpdateOnlyRepository
    {
        private readonly MiCashDbContext _dbContext;

        public SavingsGoalRepository ( MiCashDbContext dbContext )
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<SavingsGoalWithBalance>> GetAllWithBalanceAsync ( long userId , CancellationToken cancellationToken )
        {
            // Balance aggregated in the database: movements are never loaded for the list.
            var rows = await _dbContext.SavingsGoals
                .AsNoTracking()
                .Where(goal => goal.UserId == userId)
                .Select(goal => new {
                    Goal = goal ,
                    Saved = goal.Movements.Sum(m => m.Type == SavingsMovementType.Deposit ? m.Amount : -m.Amount)
                })
                .ToListAsync(cancellationToken);

            return rows
                .Select(row => new SavingsGoalWithBalance(row.Goal , row.Saved))
                .ToList();
        }

        public Task<SavingsGoal?> GetByIdWithMovementsAsync ( long userId , long id , CancellationToken cancellationToken )
        {
            return _dbContext.SavingsGoals
                .AsNoTracking()
                .Include(goal => goal.Movements)
                .FirstOrDefaultAsync(goal => goal.Id == id && goal.UserId == userId , cancellationToken);
        }

        public async Task<decimal> GetSavedAmountAsync ( long savingsGoalId , CancellationToken cancellationToken )
        {
            return await _dbContext.SavingsGoalMovements
                .Where(m => m.SavingsGoalId == savingsGoalId)
                .SumAsync(m => m.Type == SavingsMovementType.Deposit ? m.Amount : -m.Amount , cancellationToken);
        }

        Task<SavingsGoal?> ISavingsGoalUpdateOnlyRepository.GetByIdAsync ( long userId , long id , CancellationToken cancellationToken )
        {
            return _dbContext.SavingsGoals
                .FirstOrDefaultAsync(goal => goal.Id == id && goal.UserId == userId , cancellationToken);
        }

        public async Task AddAsync ( SavingsGoal goal , CancellationToken cancellationToken )
        {
            await _dbContext.SavingsGoals.AddAsync(goal , cancellationToken);
        }

        public async Task AddMovementAsync ( SavingsGoalMovement movement , CancellationToken cancellationToken )
        {
            await _dbContext.SavingsGoalMovements.AddAsync(movement , cancellationToken);
        }

        public void Delete ( SavingsGoal goal )
        {
            // Movements are removed by the database cascade.
            _dbContext.SavingsGoals.Remove(goal);
        }
    }
}
