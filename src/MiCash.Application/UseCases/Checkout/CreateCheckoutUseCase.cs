using MiCash.Communication.Requests.Checkout;
using MiCash.Communication.Responses.Checkout;
using MiCash.Domain.Entities;
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

            if ( plan is null ) {
                throw new NotFoundException(ResourceErrorMessages.PLAN_NOT_FOUND);
            }

            var activeSubscription = await _subscriptionsRepository.GetActiveByUserIdAsync( loggedUser.Id , cancellationToken);

            if ( activeSubscription is not null ) {
                throw new ErrorOnValidationException(
                [
                    "Você já possui uma assinatura ativa."
                ]);
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
