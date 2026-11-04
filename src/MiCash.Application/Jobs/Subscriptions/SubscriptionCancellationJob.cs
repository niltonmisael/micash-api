using MiCash.Application.Services.Subscriptions;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.Jobs;
using MiCash.Domain.Services.Payments;
using Microsoft.Extensions.Logging;

namespace MiCash.Application.Jobs.Subscriptions
{
    /// <summary>
    /// Runs shortly before the next billing date of a subscription whose cancellation the user requested:
    /// cancels it at Pagar.me (no more charges). The user keeps Premium until the end of the paid period, when
    /// <see cref="SubscriptionExpirationJob"/> ends the access. A gateway failure throws, so Hangfire retries.
    /// </summary>
    public class SubscriptionCancellationJob : ISubscriptionCancellationJob
    {
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly ISubscriptionsUpdateOnlyRepository _subscriptionsUpdateOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly ILogger<SubscriptionCancellationJob> _logger;

        public SubscriptionCancellationJob (
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository ,
            ISubscriptionsUpdateOnlyRepository subscriptionsUpdateOnlyRepository ,
            IUnitOfWork unitOfWork ,
            IPaymentGateway paymentGateway ,
            IBackgroundJobService backgroundJobService ,
            ILogger<SubscriptionCancellationJob> logger )
        {
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _subscriptionsUpdateOnlyRepository = subscriptionsUpdateOnlyRepository;
            _unitOfWork = unitOfWork;
            _paymentGateway = paymentGateway;
            _backgroundJobService = backgroundJobService;
            _logger = logger;
        }

        public async Task ExecuteAsync (
            long subscriptionId ,
            CancellationToken cancellationToken )
        {
            _logger.LogInformation(
                "Iniciando cancelamento agendado da subscription {SubscriptionId}" ,
                subscriptionId);

            var subscription =
                await _subscriptionsReadOnlyRepository
                    .GetByIdAsync(
                        subscriptionId ,
                        cancellationToken);

            if ( subscription is null ) {
                _logger.LogWarning(
                    "Subscription {SubscriptionId} não encontrada." ,
                    subscriptionId);

                return;
            }

            if ( subscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired ) {
                _logger.LogInformation(
                    "Subscription {SubscriptionId} já encerrada ({Status})." ,
                    subscriptionId ,
                    subscription.Status);

                return;
            }

            if ( !subscription.CancelAtPeriodEnd ) {
                _logger.LogInformation(
                    "Subscription {SubscriptionId} não possui cancelamento agendado." ,
                    subscriptionId);

                return;
            }

            if ( SubscriptionCancellationRules.ChargesStopped(subscription) ) {
                _logger.LogInformation(
                    "Subscription {SubscriptionId} já cancelada no Pagar.me; acesso até {EndsAt}." ,
                    subscriptionId ,
                    subscription.EndsAt);

                return;
            }

            if ( string.IsNullOrWhiteSpace(
                subscription.PagarmeSubscriptionId) ) {
                _logger.LogWarning(
                    "Subscription {SubscriptionId} sem PagarmeSubscriptionId." ,
                    subscriptionId);

                return;
            }

            await _paymentGateway
                .CancelSubscriptionAsync(
                    subscription.PagarmeSubscriptionId ,
                    cancellationToken);

            // This job's own id: it is finishing, not to be deleted.
            subscription.CancellationJobId = null;

            // Applied here as well: the subscription.canceled webhook may be late or lost.
            SubscriptionCancellationRules.ApplyGatewayCancellation(
                subscription ,
                gatewayCanceledAt: null ,
                DateTime.UtcNow ,
                _backgroundJobService);

            subscription.UpdatedAt = DateTime.UtcNow;

            _subscriptionsUpdateOnlyRepository.Update(subscription , cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            _logger.LogInformation(
                "Subscription {SubscriptionId} cancelada no Pagar.me. Status={Status}, acesso até {EndsAt}." ,
                subscriptionId ,
                subscription.Status ,
                subscription.EndsAt);
        }
    }
}
