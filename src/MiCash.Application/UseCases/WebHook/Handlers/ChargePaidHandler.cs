using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Subscriptions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MiCash.Application.UseCases.WebHook.Handlers
{
    public class ChargePaidHandler : IPagarMeWebhookHandler
    {
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly ISubscriptionsUpdateOnlyRepository _subscriptionsUpdateOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChargePaidHandler> _logger;

        public ChargePaidHandler (
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository ,
            ISubscriptionsUpdateOnlyRepository subscriptionsUpdateOnlyRepository ,
            IUnitOfWork unitOfWork ,
            ILogger<ChargePaidHandler> logger )
        {
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _subscriptionsUpdateOnlyRepository = subscriptionsUpdateOnlyRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public string EventName => "charge.paid";

        public async Task HandleAsync ( JsonElement payload , CancellationToken cancellationToken )
        {
            var data = payload.GetProperty("data");

            var chargeId =
                data.GetProperty("id")
                    .GetString();

            var paidAt =
                data.GetProperty("paid_at")
                    .GetString();

            var subscriptionId =
                data.GetProperty("invoice")
                    .GetProperty("subscriptionId")
                    .GetString();

            if ( string.IsNullOrWhiteSpace(subscriptionId) ) {
                _logger.LogWarning(
                    "SubscriptionId não encontrado no webhook charge.paid.");

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

            // Idempotência
            if ( subscription.Status == SubscriptionStatus.Active ) {
                _logger.LogInformation(
                    "Subscription {SubscriptionId} já está ativa." ,
                    subscriptionId);

                return;
            }

            subscription.Status = SubscriptionStatus.Active;

            subscription.PagarmeChargeId = chargeId;

            subscription.UpdatedAt = DateTime.UtcNow;

            if ( !string.IsNullOrWhiteSpace(paidAt) ) {
                subscription.StartsAt ??=
                    DateTime.Parse(paidAt);
            }

            _subscriptionsUpdateOnlyRepository.Update(subscription, cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            _logger.LogInformation(
                "Subscription {SubscriptionId} ativada com sucesso." ,
                subscriptionId);
        }
    }
}
