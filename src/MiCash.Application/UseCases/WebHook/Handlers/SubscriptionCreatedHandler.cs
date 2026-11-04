using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.PaymentLinks;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Repositories.Users;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MiCash.Application.UseCases.WebHook.Handlers
{
    public class SubscriptionCreatedHandler : IPagarMeWebhookHandler
    {
        private readonly IUserUpdateOnlyRepository _userUpdateOnlyRepository;
        private readonly IUserReadOnlyRepository _userReadOnlyRepository;
        private readonly ISubscriptionsWriteOnlyRepository _subscriptionsWriteOnlyRepository;
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly IPaymentLinkRepository _paymentLinkRepository;
        private readonly ILogger<SubscriptionCreatedHandler> _logger;

        public SubscriptionCreatedHandler (
            IUserUpdateOnlyRepository userUpdateOnlyRepository,
            IUserReadOnlyRepository userReadOnlyRepository,
            ISubscriptionsWriteOnlyRepository subscriptionsWriteOnlyRepository,
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository,
            IPaymentLinkRepository paymentLinkRepository,
            ILogger<SubscriptionCreatedHandler> logger)
        {
            _userUpdateOnlyRepository = userUpdateOnlyRepository;
            _userReadOnlyRepository = userReadOnlyRepository;
            _subscriptionsWriteOnlyRepository = subscriptionsWriteOnlyRepository;
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _paymentLinkRepository = paymentLinkRepository;
            _logger = logger;
        }

        public string EventName => "subscription.created";

        public async Task HandleAsync ( JsonElement payload , CancellationToken cancellationToken )
        {
            var data = payload.GetProperty("data");

            var pagarmeSubscriptionId = WebhookPayload.GetString(data , "id");

            if ( string.IsNullOrWhiteSpace(pagarmeSubscriptionId) ) {
                _logger.LogWarning(
                    "SubscriptionId não encontrado no webhook subscription.created.");

                return;
            }

            var exists =
                await _subscriptionsReadOnlyRepository
                    .ExistsByPagarmeSubscriptionIdAsync( pagarmeSubscriptionId , cancellationToken);

            if ( exists ) {
                _logger.LogInformation(
                    "Subscription {SubscriptionId} já processada." ,
                    pagarmeSubscriptionId);

                return;
            }

            var pagarmeCustomerId = WebhookPayload.GetString(data , "customer" , "id");

            // The payment link created by CreateCheckoutUseCase becomes the subscription "code".
            var paymentLinkId = WebhookPayload.GetString(data , "code");

            if ( string.IsNullOrWhiteSpace(paymentLinkId) ) {
                _logger.LogWarning(
                    "PaymentLinkId não encontrado no webhook da subscription {SubscriptionId}." ,
                    pagarmeSubscriptionId);

                return;
            }

            var paymentLink =
                await _paymentLinkRepository
                    .GetByPagarmePaymentLinkIdAsync(
                        paymentLinkId ,
                        cancellationToken);

            if ( paymentLink is null ) {
                _logger.LogWarning(
                    "PaymentLink {PaymentLinkId} não encontrado." ,
                    paymentLinkId);

                return;
            }

            var user =
                await _userReadOnlyRepository
                    .GetByIdAsync(
                        paymentLink.UserId ,
                        cancellationToken);

            if ( user is null ) {
                _logger.LogWarning(
                    "Usuário {UserId} do PaymentLink {PaymentLinkId} não encontrado." ,
                    paymentLink.UserId ,
                    paymentLinkId);

                return;
            }

            if ( !string.IsNullOrWhiteSpace(pagarmeCustomerId) &&
                 user.PagarMeCustomerId != pagarmeCustomerId ) {

                user.PagarMeCustomerId = pagarmeCustomerId;

                _userUpdateOnlyRepository.Update(user);
            }

            var now = DateTime.UtcNow;

            var subscription = new Subscription
            {
                UserId = paymentLink.UserId ,
                PlanId = paymentLink.PlanId ,

                PagarmeSubscriptionId = pagarmeSubscriptionId ,
                PagarmeCustomerId = pagarmeCustomerId ,

                // Becomes Active on charge.paid.
                Status = SubscriptionStatus.Pending ,

                StartsAt = WebhookPayload.GetUtcDateTime(data , "start_at") ?? now ,
                NextBillingDate = WebhookPayload.GetUtcDateTime(data , "next_billing_at") ,

                CreatedAt = now ,
                UpdatedAt = now
            };

            await _subscriptionsWriteOnlyRepository
                .AddAsync(
                    subscription ,
                    cancellationToken);

            _logger.LogInformation(
                "Subscription criada. UserId={UserId}, SubscriptionId={SubscriptionId}" ,
                subscription.UserId ,
                subscription.PagarmeSubscriptionId);
        }
    }
}
