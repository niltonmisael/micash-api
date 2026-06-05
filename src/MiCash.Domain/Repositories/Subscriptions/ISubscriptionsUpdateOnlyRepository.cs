using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Subscriptions
{
    public interface ISubscriptionsUpdateOnlyRepository
    {
        void Update ( Subscription subscription, CancellationToken cancellationToken );
    }
}
