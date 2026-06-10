using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentLinkRepository _paymentLinkRepository;
        private readonly ILogger<SubscriptionCreatedHandler> _logger;

        public SubscriptionCreatedHandler (
            IUserUpdateOnlyRepository userUpdateOnlyRepository,
            IUserReadOnlyRepository userReadOnlyRepository,
            ISubscriptionsWriteOnlyRepository subscriptionsWriteOnlyRepository,
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository,
            IUnitOfWork unitOfWork,
            IPaymentLinkRepository paymentLinkRepository,
            ILogger<SubscriptionCreatedHandler> logger)
        {
            _userUpdateOnlyRepository = userUpdateOnlyRepository;
            _userReadOnlyRepository = userReadOnlyRepository;
            _subscriptionsWriteOnlyRepository = subscriptionsWriteOnlyRepository;
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _unitOfWork = unitOfWork;
            _paymentLinkRepository = paymentLinkRepository;
            _logger = logger;
        }
        public string EventName => "subscription.created";

        public async Task HandleAsync ( JsonElement payload , CancellationToken cancellationToken )
        {
            var data = payload.GetProperty("data");

            var pagarmeSubscriptionId =
                data.GetProperty("id").GetString();

            if ( string.IsNullOrWhiteSpace(pagarmeSubscriptionId) ) {
                _logger.LogWarning(
                    "SubscriptionId não encontrado no webhook.");

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

            var pagarmeCustomerId =
                data.GetProperty("customer")
                    .GetProperty("id")
                    .GetString();

            var paymentLinkId =
                data.GetProperty("code")
                    .GetString();

            if ( string.IsNullOrWhiteSpace(paymentLinkId) ) {
                _logger.LogWarning(
                    "PaymentLinkId não encontrado no webhook.");

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

            if ( string.IsNullOrWhiteSpace(user!.PagarMeCustomerId) ||
                user.PagarMeCustomerId != pagarmeCustomerId ) {

                user.PagarMeCustomerId = pagarmeCustomerId;

                _userUpdateOnlyRepository.Update(user);
            }

            var subscription = new Subscription
            {
                UserId = paymentLink.UserId ,
                PlanId = paymentLink.PlanId ,

                PagarmeSubscriptionId = pagarmeSubscriptionId ,
                PagarmeCustomerId = pagarmeCustomerId ,

                Status = SubscriptionStatus.Pending ,

                StartsAt = DateTime.SpecifyKind(
                    DateTime.Parse(
                        data.GetProperty("start_at")
                        .GetString()!) ,
                    DateTimeKind.Utc),

                NextBillingDate = DateTime.SpecifyKind(
                    DateTime.Parse(
                        data.GetProperty("next_billing_at")
                        .GetString()!) ,
                    DateTimeKind.Utc),

                CreatedAt = DateTime.UtcNow ,
                UpdatedAt = DateTime.UtcNow
            };

            await _subscriptionsWriteOnlyRepository
                .AddAsync(
                    subscription ,
                    cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            _logger.LogInformation(
                "Subscription criada. UserId={UserId}, SubscriptionId={SubscriptionId}" ,
                subscription.UserId ,
                subscription.PagarmeSubscriptionId);
        }
    }
}
