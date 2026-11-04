using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Subscriptions
{
    public interface ISubscriptionsReadOnlyRepository
    {
        Task<Subscription?> GetActiveByUserIdAsync ( long userId , CancellationToken cancellationToken);
        Task<Subscription?> GetByIdAsync ( long id , CancellationToken cancellationToken);
        Task<Subscription?> GetCurrentByUserIdAsync ( long userId, CancellationToken cancellationToken );

        /// <summary>
        /// The subscription that may give the user Premium (with its Plan): the Active one, otherwise the latest
        /// PaymentFailed (grace period, see SubscriptionAccess). Null when there is none.
        /// </summary>
        Task<Subscription?> GetWithAccessByUserIdAsync ( long userId , CancellationToken cancellationToken );
        Task<Subscription?> GetByPagarmeSubscriptionIdAsync ( string subscriptionId , CancellationToken cancellationToken );
        Task<bool> ExistsByPagarmeSubscriptionIdAsync ( string subscriptionId ,CancellationToken cancellationToken );
        Task<List<Subscription>> GetHistoryByUserIdAsync (long userId , CancellationToken cancellationToken );

        /// <summary>
        /// Active subscriptions whose next billing date is before <paramref name="billingDateBefore"/>: the renewal
        /// should already have been reported by webhook, so they are checked against the gateway.
        /// </summary>
        Task<List<Subscription>> GetActiveWithBillingDateBeforeAsync ( DateTime billingDateBefore , CancellationToken cancellationToken );

        /// <summary>Canceled subscriptions still Active whose paid period (EndsAt) is over.</summary>
        Task<List<Subscription>> GetActiveEndedBeforeAsync ( DateTime endedBefore , CancellationToken cancellationToken );
    }
}
