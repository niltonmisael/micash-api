using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.PaymentLinks;
using MiCash.Domain.Repositories.Payments;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Repositories.Users;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MiCash.Application.UseCases.WebHook.Handlers
{
    /// <summary>
    /// A contracting finished at the hosted checkout: registers the subscription (Pending until charge.paid) for the
    /// account holder of the payment link, and the payer (gateway customer) who paid it. The payer may be another
    /// person; it is reused when the same gateway customer pays this user again.
    /// </summary>
    public class SubscriptionCreatedHandler : IPagarMeWebhookHandler
    {
        private readonly IPaymentCustomerRepository _paymentCustomerRepository;
        private readonly IUserReadOnlyRepository _userReadOnlyRepository;
        private readonly ISubscriptionsWriteOnlyRepository _subscriptionsWriteOnlyRepository;
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly IPaymentLinkRepository _paymentLinkRepository;
        private readonly ILogger<SubscriptionCreatedHandler> _logger;

        public SubscriptionCreatedHandler (
            IPaymentCustomerRepository paymentCustomerRepository,
            IUserReadOnlyRepository userReadOnlyRepository,
            ISubscriptionsWriteOnlyRepository subscriptionsWriteOnlyRepository,
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository,
            IPaymentLinkRepository paymentLinkRepository,
            ILogger<SubscriptionCreatedHandler> logger)
        {
            _paymentCustomerRepository = paymentCustomerRepository;
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

            var now = DateTime.UtcNow;

            var payer = await GetOrAddPayerAsync(user.Id , data , now , cancellationToken);

            var subscription = new Subscription
            {
                UserId = paymentLink.UserId ,
                PlanId = paymentLink.PlanId ,

                PagarmeSubscriptionId = pagarmeSubscriptionId ,
                // Fixed for this subscription: a later contracting by another payer never changes it.
                PaymentCustomer = payer ,

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

        /// <summary>
        /// The payer row of this user for the gateway customer of the event, created on first use. Identified by
        /// the gateway id only (never by e-mail: several people may share one, and one person may change it).
        /// Null when the event carries no customer.
        /// </summary>
        private async Task<PaymentCustomer?> GetOrAddPayerAsync (
            long userId ,
            JsonElement data ,
            DateTime now ,
            CancellationToken cancellationToken )
        {
            var customerId = WebhookPayload.GetString(data , "customer" , "id");

            if ( string.IsNullOrWhiteSpace(customerId) ) {
                return null;
            }

            var payer = await _paymentCustomerRepository.GetAsync(
                userId , PaymentCustomer.PagarmeProvider , customerId , cancellationToken);

            if ( payer is null ) {
                payer = new PaymentCustomer
                {
                    UserId = userId ,
                    Provider = PaymentCustomer.PagarmeProvider ,
                    ExternalCustomerId = customerId ,
                    FirstUsedAt = now ,
                    CreatedAt = now
                };

                await _paymentCustomerRepository.AddAsync(payer , cancellationToken);
            }

            payer.Name = WebhookPayload.GetString(data , "customer" , "name") ?? payer.Name;
            payer.Email = WebhookPayload.GetString(data , "customer" , "email") ?? payer.Email;
            payer.LastUsedAt = now;
            payer.UpdatedAt = now;

            return payer;
        }
    }
}
