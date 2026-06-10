using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Subscriptions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MiCash.Application.UseCases.WebHook.Handlers
{
    public class ChargePaymentFailedHandler : IPagarMeWebhookHandler
    {
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly ISubscriptionsUpdateOnlyRepository _subscriptionsUpdateOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChargePaymentFailedHandler> _logger;

        public ChargePaymentFailedHandler (
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository ,
            ISubscriptionsUpdateOnlyRepository subscriptionsUpdateOnlyRepository ,
            IUnitOfWork unitOfWork ,
            ILogger<ChargePaymentFailedHandler> logger )
        {
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _subscriptionsUpdateOnlyRepository = subscriptionsUpdateOnlyRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public string EventName => "charge.payment_failed";

        public async Task HandleAsync (
            JsonElement payload ,
            CancellationToken cancellationToken )
        {
            var data = payload.GetProperty("data");

            var chargeId =
                data.GetProperty("id")
                    .GetString();

            var subscriptionId =
                data.GetProperty("invoice")
                    .GetProperty("subscriptionId")
                    .GetString();

            if ( string.IsNullOrWhiteSpace(subscriptionId) ) {
                _logger.LogWarning(
                    "SubscriptionId não encontrado no webhook charge.payment_failed.");

                return;
            }

            var subscription =
                await _subscriptionsReadOnlyRepository
                    .GetByPagarmeSubscriptionIdAsync(
                        subscriptionId ,
                        cancellationToken);

            if ( subscription is null ) {
                _logger.LogWarning(
                    "Subscription {SubscriptionId} não encontrada." ,
                    subscriptionId);

                return;
            }

            if ( subscription.Status == SubscriptionStatus.Cancelled ) {
                _logger.LogInformation(
                    "Subscription {SubscriptionId} já cancelada. Ignorando charge.payment_failed." ,
                    subscriptionId);

                return;
            }

            string? failureReason = null;

            try {
                failureReason =
                    data.GetProperty("last_transaction")
                        .GetProperty("gateway_response")
                        .GetProperty("errors")[0]
                        .GetProperty("message")
                        .GetString();
            } catch (System.Exception ex) {
                _logger.LogWarning(
                    ex,
                    "Não foi possível obter o motivo da falha para a subscription {SubscriptionId}" ,
                    subscriptionId);
            }

            subscription.Status = SubscriptionStatus.PaymentFailed;

            subscription.PagarmeChargeId = chargeId;

            subscription.UpdatedAt = DateTime.UtcNow;

            _subscriptionsUpdateOnlyRepository.Update(subscription, cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            _logger.LogWarning(
                "Pagamento da Subscription {SubscriptionId} falhou. Motivo: {Reason}" ,
                subscriptionId ,
                failureReason ?? "Motivo não informado");
        }
    }
}