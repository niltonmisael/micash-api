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

        public async Task<bool> ExistsByPagarmeSubscriptionIdAsync ( string subscriptionId , CancellationToken cancellationToken )
        {
            return await _dbContext.Subscriptions
                .AnyAsync(x => x.PagarmeSubscriptionId == subscriptionId ,cancellationToken);
        }

        public async Task<Subscription?> GetActiveByUserIdAsync ( long userId , CancellationToken cancellationToken )
        {
            return await _dbContext.Subscriptions
                .Where(x =>
                    x.UserId == userId &&
                    x.Status == SubscriptionStatus.Active)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Subscription?> GetByIdAsync ( long id , CancellationToken cancellationToken )
        {
            return await _dbContext.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<Subscription?> GetByPagarmeSubscriptionIdAsync ( string subscriptionId , CancellationToken cancellationToken )
        {
            return await _dbContext.Subscriptions.FirstOrDefaultAsync(s => s.PagarmeSubscriptionId == subscriptionId , cancellationToken);
        }

        public async Task<Subscription?> GetCurrentByUserIdAsync ( long userId , CancellationToken cancellationToken )
        {
            return await _dbContext
                .Subscriptions
                .AsNoTracking()
                .Include(x => x.Plan)
                .Where(x =>
                     x.UserId == userId &&
                     ( x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.Pending ))
                // An active subscription wins over a pending checkout (e.g. plan change in progress).
                .OrderByDescending(x => x.Status == SubscriptionStatus.Active)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Subscription?> GetWithAccessByUserIdAsync ( long userId , CancellationToken cancellationToken )
        {
            return await _dbContext
                .Subscriptions
                .AsNoTracking()
                .Include(x => x.Plan)
                .Where(x =>
                     x.UserId == userId &&
                     ( x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.PaymentFailed ))
                .OrderByDescending(x => x.Status == SubscriptionStatus.Active)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<Subscription>> GetHistoryByUserIdAsync ( long userId , CancellationToken cancellationToken )
        {
            return await _dbContext.Subscriptions
                .Include(x => x.Plan)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Subscription>> GetActiveWithBillingDateBeforeAsync ( DateTime billingDateBefore , CancellationToken cancellationToken )
        {
            return await _dbContext.Subscriptions
                .AsNoTracking()
                .Where(x =>
                    x.Status == SubscriptionStatus.Active &&
                    x.PagarmeSubscriptionId != null &&
                    x.NextBillingDate != null &&
                    x.NextBillingDate < billingDateBefore)
                .OrderBy(x => x.NextBillingDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Subscription>> GetActiveEndedBeforeAsync ( DateTime endedBefore , CancellationToken cancellationToken )
        {
            return await _dbContext.Subscriptions
                .AsNoTracking()
                .Where(x =>
                    x.Status == SubscriptionStatus.Active &&
                    x.EndsAt != null &&
                    x.EndsAt <= endedBefore)
                .ToListAsync(cancellationToken);
        }

        public void Update ( Subscription subscription , CancellationToken cancellationToken )
        {
            _dbContext.Subscriptions.Update(subscription);
        }
    }
}
