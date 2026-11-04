using MiCash.Communication.Requests.Checkout;
using MiCash.Communication.Responses.Checkout;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.PaymentLinks;
using MiCash.Domain.Repositories.Plans;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Domain.Services.Payments;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Checkout
{
    public class CreateCheckoutUseCase : ICreateCheckoutUseCase
    {
        /// <summary>How long a Pending subscription (payment awaiting confirmation) blocks a new checkout.</summary>
        public static readonly TimeSpan PendingPaymentWindow = TimeSpan.FromHours(1);

        private readonly ILoggedUser _loggedUser;
        private readonly IPlansReadOnlyRepository _plansRepository;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsRepository;
        private readonly IPaymentLinkRepository _paymentLinkRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateCheckoutUseCase (
            ILoggedUser loggedUser,
            IPlansReadOnlyRepository plansRepository,
            IPaymentGateway paymentGateway,
            ISubscriptionsReadOnlyRepository subscriptionsRepository,
            IPaymentLinkRepository paymentLinkRepository,
            IUnitOfWork unitOfWork
            )
        {
            _loggedUser = loggedUser;
            _plansRepository = plansRepository;
            _paymentGateway = paymentGateway;
            _subscriptionsRepository = subscriptionsRepository;
            _paymentLinkRepository = paymentLinkRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseCheckoutJson> ExecuteAsync ( RequestCreateCheckoutJson request , CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();

            var plan = await _plansRepository.GetByIdAsync(request.PlanId, cancellationToken);

            // A plan withdrawn from sale is not offered anymore.
            if ( plan is null || !plan.IsActive || string.IsNullOrWhiteSpace(plan.PagarmePlanId) ) {
                throw new NotFoundException(ResourceErrorMessages.PLAN_NOT_FOUND);
            }

            // Active first, then the most recent Pending.
            var currentSubscription = await _subscriptionsRepository.GetCurrentByUserIdAsync(loggedUser.Id , cancellationToken);

            if ( currentSubscription?.Status == SubscriptionStatus.Active ) {
                throw new ErrorOnValidationException([ResourceErrorMessages.USER_ALREADY_HAS_ACTIVE_SUBSCRIPTION]);
            }

            // Payment just made and not confirmed yet: a second checkout would charge the user twice.
            // Time-boxed so a subscription stuck in Pending never blocks the user for good.
            if ( currentSubscription?.Status == SubscriptionStatus.Pending &&
                 currentSubscription.CreatedAt > DateTime.UtcNow - PendingPaymentWindow ) {
                throw new ErrorOnValidationException([ResourceErrorMessages.SUBSCRIPTION_PAYMENT_IN_PROGRESS]);
            }

            var checkout = await _paymentGateway.CreateSubscriptionCheckoutAsync(loggedUser , plan , cancellationToken);

            await _paymentLinkRepository.AddAsync(new PaymentLink
            {
                UserId = loggedUser.Id ,
                PlanId = plan.Id ,
                OrderCode = checkout.OrderCode ,
                PagarmePaymentLinkId = checkout.PaymentLinkId ,
                CheckoutUrl = checkout.CheckoutUrl ,
                Status = "active"
            }, cancellationToken);

            await _unitOfWork.Commit(cancellationToken);


            return new ResponseCheckoutJson { CheckoutUrl = checkout.CheckoutUrl };
        }
    }
}
