using MiCash.Application.Services.Subscriptions;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.Payments;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.Jobs;
using MiCash.Domain.Services.Payments;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MiCash.Application.UseCases.WebHook.Handlers
{
    /// <summary>
    /// charge.refunded: the money of a subscription charge went back to the customer, so Premium ends NOW
    /// (business rule: a refund does not keep the paid period). The subscription is also canceled at Pagar.me first,
    /// otherwise the next cycle would charge again a user who no longer has access. If that call fails the handler
    /// throws and Pagar.me delivers the event again (nothing is saved half-way).
    /// The subscription.canceled that Pagar.me sends afterwards finds it already Cancelled and does nothing.
    /// Partial refunds (charge.partial_canceled) are not handled: access is kept.
    /// </summary>
    public class ChargeRefundedHandler : IPagarMeWebhookHandler
    {
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly ISubscriptionsUpdateOnlyRepository _subscriptionsUpdateOnlyRepository;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IPaymentTransactionRepository _transactions;
        private readonly TimeProvider _time;
        private readonly ILogger<ChargeRefundedHandler> _logger;

        public ChargeRefundedHandler (
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository ,
            ISubscriptionsUpdateOnlyRepository subscriptionsUpdateOnlyRepository ,
            IPaymentGateway paymentGateway ,
            IBackgroundJobService backgroundJobService ,
            IPaymentTransactionRepository transactions ,
            TimeProvider time ,
            ILogger<ChargeRefundedHandler> logger )
        {
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _subscriptionsUpdateOnlyRepository = subscriptionsUpdateOnlyRepository;
            _paymentGateway = paymentGateway;
            _backgroundJobService = backgroundJobService;
            _transactions = transactions;
            _time = time;
            _logger = logger;
        }

        public virtual string EventName => "charge.refunded";

        protected virtual PaymentTransactionStatus TransactionStatus => PaymentTransactionStatus.Refunded;

        public async Task HandleAsync ( JsonElement payload , CancellationToken cancellationToken )
        {
            var data = payload.GetProperty("data");

            var chargeId = WebhookPayload.GetString(data , "id");
            var subscriptionId = ChargeWebhook.GetSubscriptionId(data);

            if ( string.IsNullOrWhiteSpace(subscriptionId) ) {
                _logger.LogInformation("{EventType} {ChargeId} sem assinatura vinculada; ignorado." , EventName , chargeId);

                return;
            }

            var subscription =
                await _subscriptionsReadOnlyRepository
                    .GetByPagarmeSubscriptionIdAsync(
                        subscriptionId ,
                        cancellationToken);

            if ( subscription is null ) {
                _logger.LogWarning("{EventType} {ChargeId}: Subscription {SubscriptionId} não encontrada." , EventName , chargeId , subscriptionId);

                return;
            }

            await ChargeWebhook.RecordAsync(
                _transactions ,
                subscription ,
                data ,
                TransactionStatus ,
                _time.GetUtcNow().UtcDateTime ,
                cancellationToken);

            if ( subscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired ) {
                _logger.LogInformation("{EventType} {ChargeId}: Subscription {SubscriptionId} já encerrada." , EventName , chargeId , subscriptionId);

                return;
            }

            // Stop future charges before ending the access (idempotent at the gateway).
            await _paymentGateway.CancelSubscriptionAsync(subscriptionId , cancellationToken);

            var now = _time.GetUtcNow().UtcDateTime;

            SubscriptionCancellationRules.EndNow(subscription , now , _backgroundJobService);
            subscription.UpdatedAt = now;

            _subscriptionsUpdateOnlyRepository.Update(subscription , cancellationToken);

            _logger.LogWarning(
                "{EventType} {ChargeId}: Premium da Subscription {SubscriptionId} encerrado imediatamente e cobranças canceladas." ,
                EventName ,
                chargeId ,
                subscriptionId);
        }
    }

    /// <summary>charge.chargedback (contestação no cartão): same effect as a refund.</summary>
    public class ChargeChargedbackHandler : ChargeRefundedHandler
    {
        public ChargeChargedbackHandler (
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository ,
            ISubscriptionsUpdateOnlyRepository subscriptionsUpdateOnlyRepository ,
            IPaymentGateway paymentGateway ,
            IBackgroundJobService backgroundJobService ,
            IPaymentTransactionRepository transactions ,
            TimeProvider time ,
            ILogger<ChargeRefundedHandler> logger )
            : base(subscriptionsReadOnlyRepository , subscriptionsUpdateOnlyRepository , paymentGateway , backgroundJobService , transactions , time , logger)
        {
        }

        public override string EventName => "charge.chargedback";

        protected override PaymentTransactionStatus TransactionStatus => PaymentTransactionStatus.Chargedback;
    }
}
