using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.Jobs;
using MiCash.Domain.Services.Payments;
using Microsoft.Extensions.Logging;

namespace MiCash.Application.Jobs.Subscriptions
{
    public class SubscriptionCancellationJob : ISubscriptionCancellationJob
    {
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ILogger<SubscriptionCancellationJob> _logger;

        public SubscriptionCancellationJob (
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository ,
            IPaymentGateway paymentGateway ,
            ILogger<SubscriptionCancellationJob> logger )
        {
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _paymentGateway = paymentGateway;
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

            if ( !subscription.CancelAtPeriodEnd ) {
                _logger.LogInformation(
                    "Subscription {SubscriptionId} não possui cancelamento agendado." ,
                    subscriptionId);

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

            _logger.LogInformation(
                "Cancelamento enviado ao Pagar.me para Subscription {SubscriptionId}" ,
                subscriptionId);
        }
    }
}
