using MiCash.Application.Services.Subscriptions;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.Jobs;
using Microsoft.Extensions.Logging;

namespace MiCash.Application.Jobs.Subscriptions
{
    /// <summary>
    /// End of the paid period of a canceled subscription: the user goes back to Free.
    /// The daily reconciliation does the same for subscriptions whose job was lost.
    /// </summary>
    public class SubscriptionExpirationJob : ISubscriptionExpirationJob
    {
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly ISubscriptionsUpdateOnlyRepository _subscriptionsUpdateOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<SubscriptionExpirationJob> _logger;

        public SubscriptionExpirationJob (
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository ,
            ISubscriptionsUpdateOnlyRepository subscriptionsUpdateOnlyRepository ,
            IUnitOfWork unitOfWork ,
            TimeProvider timeProvider ,
            ILogger<SubscriptionExpirationJob> logger )
        {
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _subscriptionsUpdateOnlyRepository = subscriptionsUpdateOnlyRepository;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task ExecuteAsync ( long subscriptionId , CancellationToken cancellationToken )
        {
            var subscription = await _subscriptionsReadOnlyRepository.GetByIdAsync(subscriptionId , cancellationToken);

            if ( subscription is null || subscription.Status != SubscriptionStatus.Active ) {
                return;
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;

            if ( subscription.EndsAt is not { } endsAt || endsAt > now ) {
                // Reactivated, or the period was moved: nothing to end now.
                _logger.LogInformation(
                    "Subscription {SubscriptionId} sem término vencido (EndsAt={EndsAt}); nada a encerrar." ,
                    subscriptionId ,
                    subscription.EndsAt);

                return;
            }

            SubscriptionCancellationRules.Close(subscription , endsAt);
            subscription.UpdatedAt = now;

            _subscriptionsUpdateOnlyRepository.Update(subscription , cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            _logger.LogInformation(
                "Período pago da Subscription {SubscriptionId} encerrado em {EndsAt}; usuário volta ao plano Free." ,
                subscriptionId ,
                endsAt);
        }
    }
}
