using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Subscriptions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MiCash.Application.UseCases.WebHook.Handlers
{
    public class SubscriptionCanceledHandler : IPagarMeWebhookHandler
    {
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly ISubscriptionsUpdateOnlyRepository _subscriptionsUpdateOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SubscriptionCanceledHandler> _logger;

        public SubscriptionCanceledHandler (
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository ,
            ISubscriptionsUpdateOnlyRepository subscriptionsUpdateOnlyRepository ,
            IUnitOfWork unitOfWork ,
            ILogger<SubscriptionCanceledHandler> logger )
        {
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _subscriptionsUpdateOnlyRepository = subscriptionsUpdateOnlyRepository;
            _unitOfWork = unitOfWork;
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

            var pagarmeSubscriptionId =
                data.GetProperty("id")
                    .GetString();

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

            DateTime? canceledAt = null;

            if ( data.TryGetProperty(
                    "canceled_at" ,
                    out var canceledAtElement) ) {
                canceledAt =
                    DateTime.SpecifyKind(
                        canceledAtElement.GetDateTime() ,
                        DateTimeKind.Utc);
            }

            subscription.Status =
                SubscriptionStatus.Cancelled;

            subscription.EndsAt =
                canceledAt ?? DateTime.UtcNow;

            subscription.CancelAtPeriodEnd =
                false;

            subscription.CancellationRequestedAt =
                null;

            subscription.CancellationScheduledFor =
                null;

            subscription.CancellationJobId =
                null;

            subscription.UpdatedAt =
                DateTime.UtcNow;

            _subscriptionsUpdateOnlyRepository.Update(
                subscription ,
                cancellationToken);

            await _unitOfWork.Commit(
                cancellationToken);

            _logger.LogInformation(
                "Subscription {SubscriptionId} cancelada com sucesso." ,
                pagarmeSubscriptionId);
        }
    }
}