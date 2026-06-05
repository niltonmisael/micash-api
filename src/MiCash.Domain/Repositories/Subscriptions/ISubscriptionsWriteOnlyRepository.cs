using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Subscriptions
{
    public interface ISubscriptionsWriteOnlyRepository
    {
        Task AddAsync ( Subscription subscription , CancellationToken cancellationToken);
    }
}
