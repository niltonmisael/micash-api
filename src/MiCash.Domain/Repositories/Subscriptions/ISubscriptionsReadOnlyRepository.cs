using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Subscriptions
{
    public interface ISubscriptionsReadOnlyRepository
    {
        Task<Subscription?> GetActiveByUserIdAsync ( long userId , CancellationToken cancellationToken);
        Task<Subscription?> GetByIdAsync ( long id , CancellationToken cancellationToken);
        Task<Subscription?> GetCurrentByUserIdAsync ( long userId, CancellationToken cancellationToken );
        Task<Subscription?> GetByPagarmeSubscriptionIdAsync ( string subscriptionId , CancellationToken cancellationToken );
        Task<bool> ExistsByPagarmeSubscriptionIdAsync ( string subscriptionId ,CancellationToken cancellationToken );

    }
}
