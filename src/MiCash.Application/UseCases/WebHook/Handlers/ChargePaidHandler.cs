using MiCash.Application.Services.Subscriptions;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.Payments;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.Payments;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MiCash.Application.UseCases.WebHook.Handlers
{
    /// <summary>
    /// First charge (activation) and every renewal. Each paid charge refreshes the next billing date, which
    /// the cancellation schedule and the app depend on.
    /// </summary>
    public class ChargePaidHandler : IPagarMeWebhookHandler
    {
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly ISubscriptionsUpdateOnlyRepository _subscriptionsUpdateOnlyRepository;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IPaymentTransactionRepository _transactions;
        private readonly ILogger<ChargePaidHandler> _logger;

        public ChargePaidHandler (
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository ,
            ISubscriptionsUpdateOnlyRepository subscriptionsUpdateOnlyRepository ,
            IPaymentGateway paymentGateway ,
            IPaymentTransactionRepository transactions ,
            ILogger<ChargePaidHandler> logger )
        {
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _subscriptionsUpdateOnlyRepository = subscriptionsUpdateOnlyRepository;
            _paymentGateway = paymentGateway;
            _transactions = transactions;
            _logger = logger;
        }

        public string EventName => "charge.paid";

        public async Task HandleAsync ( JsonElement payload , CancellationToken cancellationToken )
        {
            var data = payload.GetProperty("data");

            var chargeId = WebhookPayload.GetString(data , "id");
            var paidAt = WebhookPayload.GetUtcDateTime(data , "paid_at");
            var subscriptionId = ChargeWebhook.GetSubscriptionId(data);

            if ( string.IsNullOrWhiteSpace(subscriptionId) ) {
                _logger.LogInformation(
                    "charge.paid {ChargeId} sem assinatura vinculada; ignorado." ,
                    chargeId);

                return;
            }

            var subscription =
                await _subscriptionsReadOnlyRepository
                    .GetByPagarmeSubscriptionIdAsync(
                        subscriptionId ,
                        cancellationToken);

            if ( subscription is null ) {
                // charge.paid can arrive before subscription.created: let Pagar.me deliver it again.
                throw new WebhookNotReadyException(
                    $"Subscription {subscriptionId} ainda não registrada.");
            }

            await ChargeWebhook.RecordAsync(_transactions , subscription , data , PaymentTransactionStatus.Paid , DateTime.UtcNow , cancellationToken);

            if ( subscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired ) {
                _logger.LogWarning(
                    "Cobrança {ChargeId} paga para a Subscription {SubscriptionId} já encerrada ({Status}). Verificar estorno." ,
                    chargeId ,
                    subscriptionId ,
                    subscription.Status);

                return;
            }

            if ( SubscriptionCancellationRules.ChargesStopped(subscription) ) {
                // Renewal charged before the cancellation reached Pagar.me: the access still ends as promised.
                _logger.LogWarning(
                    "Cobrança {ChargeId} paga para a Subscription {SubscriptionId} já cancelada (acesso até {EndsAt}). Verificar estorno." ,
                    chargeId ,
                    subscriptionId ,
                    subscription.EndsAt);

                return;
            }

            var wasActive = subscription.Status == SubscriptionStatus.Active;

            subscription.Status = SubscriptionStatus.Active;
            // A paid retry ends the grace period of a refused renewal.
            subscription.PaymentGraceUntil = null;
            subscription.StartsAt ??= paidAt ?? DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;

            await RefreshNextBillingDateAsync(subscription , subscriptionId , cancellationToken);

            _subscriptionsUpdateOnlyRepository.Update(subscription, cancellationToken);

            _logger.LogInformation(
                wasActive
                    ? "Subscription {SubscriptionId} renovada. Próxima cobrança: {NextBillingDate}"
                    : "Subscription {SubscriptionId} ativada. Próxima cobrança: {NextBillingDate}" ,
                subscriptionId ,
                subscription.NextBillingDate);
        }

        private async Task RefreshNextBillingDateAsync (
            Domain.Entities.Subscription subscription ,
            string subscriptionId ,
            CancellationToken cancellationToken )
        {
            try {
                var remote = await _paymentGateway.GetSubscriptionAsync(subscriptionId , cancellationToken);

                if ( remote?.NextBillingAt is { } nextBillingAt &&
                     ( subscription.NextBillingDate is null || nextBillingAt > subscription.NextBillingDate ) ) {
                    subscription.NextBillingDate = nextBillingAt;
                }
            } catch ( System.Exception ex ) when ( ex is not OperationCanceledException ) {
                // The payment is confirmed: activating the user must not depend on the gateway API being up.
                // The daily reconciliation job fixes the billing date later.
                _logger.LogWarning(
                    ex ,
                    "Não foi possível atualizar a próxima cobrança da Subscription {SubscriptionId}." ,
                    subscriptionId);
            }
        }
    }
}
