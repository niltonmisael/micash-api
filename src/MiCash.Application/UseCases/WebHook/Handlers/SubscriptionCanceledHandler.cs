using MiCash.Application.Services.Subscriptions;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.Jobs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MiCash.Application.UseCases.WebHook.Handlers
{
    public class SubscriptionCanceledHandler : IPagarMeWebhookHandler
    {
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly ISubscriptionsUpdateOnlyRepository _subscriptionsUpdateOnlyRepository;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly ILogger<SubscriptionCanceledHandler> _logger;

        public SubscriptionCanceledHandler (
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository ,
            ISubscriptionsUpdateOnlyRepository subscriptionsUpdateOnlyRepository ,
            IBackgroundJobService backgroundJobService ,
            ILogger<SubscriptionCanceledHandler> logger )
        {
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _subscriptionsUpdateOnlyRepository = subscriptionsUpdateOnlyRepository;
            _backgroundJobService = backgroundJobService;
            _logger = logger;
        }

        public string EventName =>
            "subscription.canceled";

        public async Task HandleAsync (
            JsonElement payload ,
            CancellationToken cancellationToken )
        {
            var data =
                payload.GetProperty("data");

            var pagarmeSubscriptionId = WebhookPayload.GetString(data , "id");

            if ( string.IsNullOrWhiteSpace(
                pagarmeSubscriptionId) ) {
                _logger.LogWarning(
                    "Webhook subscription.canceled sem SubscriptionId.");

                return;
            }

            var subscription =
                await _subscriptionsReadOnlyRepository
                    .GetByPagarmeSubscriptionIdAsync(
                        pagarmeSubscriptionId ,
                        cancellationToken);

            if ( subscription is null ) {
                _logger.LogWarning(
                    "Subscription {SubscriptionId} não encontrada." ,
                    pagarmeSubscriptionId);

                return;
            }

            if ( subscription.Status == SubscriptionStatus.Cancelled ) {
                // Already closed locally (e.g. by SubscriptionCancellationJob): keep its EndsAt.
                _logger.LogInformation(
                    "Subscription {SubscriptionId} já cancelada." ,
                    pagarmeSubscriptionId);

                return;
            }

            // Charges stopped. A paid period is honored (Premium until it ends, then the expiration job);
            // otherwise the subscription ends now. Also covers cancellations made at the Pagar.me dashboard.
            SubscriptionCancellationRules.ApplyGatewayCancellation(
                subscription ,
                WebhookPayload.GetUtcDateTime(data , "canceled_at") ,
                DateTime.UtcNow ,
                _backgroundJobService);

            subscription.UpdatedAt =
                DateTime.UtcNow;

            _subscriptionsUpdateOnlyRepository.Update(
                subscription ,
                cancellationToken);

            _logger.LogInformation(
                "Subscription {SubscriptionId} cancelada no Pagar.me. Status={Status}, acesso até {EndsAt}." ,
                pagarmeSubscriptionId ,
                subscription.Status ,
                subscription.EndsAt);
        }
    }
}
