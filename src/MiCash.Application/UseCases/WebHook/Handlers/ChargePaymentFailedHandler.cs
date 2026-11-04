using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.Subscriptions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MiCash.Application.UseCases.WebHook.Handlers
{
    public class ChargePaymentFailedHandler : IPagarMeWebhookHandler
    {
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly ISubscriptionsUpdateOnlyRepository _subscriptionsUpdateOnlyRepository;
        private readonly ILogger<ChargePaymentFailedHandler> _logger;

        public ChargePaymentFailedHandler (
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository ,
            ISubscriptionsUpdateOnlyRepository subscriptionsUpdateOnlyRepository ,
            ILogger<ChargePaymentFailedHandler> logger )
        {
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _subscriptionsUpdateOnlyRepository = subscriptionsUpdateOnlyRepository;
            _logger = logger;
        }

        public string EventName => "charge.payment_failed";

        public async Task HandleAsync (
            JsonElement payload ,
            CancellationToken cancellationToken )
        {
            var data = payload.GetProperty("data");

            var chargeId = WebhookPayload.GetString(data , "id");
            var subscriptionId = ChargeWebhook.GetSubscriptionId(data);

            if ( string.IsNullOrWhiteSpace(subscriptionId) ) {
                _logger.LogInformation(
                    "charge.payment_failed {ChargeId} sem assinatura vinculada; ignorado." ,
                    chargeId);

                return;
            }

            var subscription =
                await _subscriptionsReadOnlyRepository
                    .GetByPagarmeSubscriptionIdAsync(
                        subscriptionId ,
                        cancellationToken);

            if ( subscription is null ) {
                // Can arrive before subscription.created: without the retry the subscription would stay Pending.
                throw new WebhookNotReadyException(
                    $"Subscription {subscriptionId} ainda não registrada.");
            }

            if ( subscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired ) {
                _logger.LogInformation(
                    "Subscription {SubscriptionId} já encerrada. Ignorando charge.payment_failed." ,
                    subscriptionId);

                return;
            }

            var failureReason = GetFailureReason(data);

            // Pagar.me keeps retrying the charge; a later charge.paid makes the subscription Active again.
            subscription.Status = SubscriptionStatus.PaymentFailed;
            subscription.PagarmeChargeId = chargeId;
            subscription.UpdatedAt = DateTime.UtcNow;

            _subscriptionsUpdateOnlyRepository.Update(subscription, cancellationToken);

            _logger.LogWarning(
                "Pagamento da Subscription {SubscriptionId} falhou. Motivo: {Reason}" ,
                subscriptionId ,
                failureReason ?? "Motivo não informado");
        }

        private static string? GetFailureReason ( JsonElement data )
        {
            var errors = WebhookPayload.Get(data , "last_transaction" , "gateway_response" , "errors");

            if ( errors is { ValueKind: JsonValueKind.Array } array && array.GetArrayLength() > 0 ) {
                return WebhookPayload.GetString(array[0] , "message");
            }

            return WebhookPayload.GetString(data , "last_transaction" , "acquirer_message");
        }
    }
}
