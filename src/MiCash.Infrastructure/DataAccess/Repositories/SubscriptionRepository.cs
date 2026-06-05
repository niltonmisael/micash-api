using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    internal class SubscriptionRepository : 
        ISubscriptionsReadOnlyRepository, ISubscriptionsWriteOnlyRepository, ISubscriptionsUpdateOnlyRepository
    {

        private readonly MiCashDbContext _dbContext;

        public SubscriptionRepository (MiCashDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task AddAsync ( Subscription subscription , CancellationToken cancellationToken )
        {
            await _dbContext.Subscriptions.AddAsync(subscription , cancellationToken);
        }

        public async Task<Subscription?> GetActiveByUserIdAsync ( long userId , CancellationToken cancellationToken )
        {
            return await _dbContext.Subscriptions
                .AsNoTracking() 
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SubscriptionStatus.Active , cancellationToken);
        }

        public async Task<Subscription?> GetByIdAsync ( long id , CancellationToken cancellationToken )
        {
            return await _dbContext.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<Subscription?> GetCurrentByUserIdAsync ( long userId , CancellationToken cancellationToken )
        {
            return await _dbContext
                .Subscriptions
                .AsNoTracking()
                .Include(x => x.Plan)
                .FirstOrDefaultAsync(x =>
                     x.UserId == userId &&
                     x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.Pending);
        }

        public void Update ( Subscription subscription , CancellationToken cancellationToken )
        {
            _dbContext.Subscriptions.Update(subscription);
        }
    }
}
