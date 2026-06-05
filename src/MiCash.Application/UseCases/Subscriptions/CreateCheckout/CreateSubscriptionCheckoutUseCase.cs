using DocumentFormat.OpenXml.Spreadsheet;
using MiCash.Communication.Requests.Subscriptions;
using MiCash.Communication.Responses.Subscriptions;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Plans;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Domain.Services.Payments;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Subscriptions.CreateCheckout
{
    public class CreateSubscriptionCheckoutUseCase : ICreateSubscriptionCheckoutUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ILoggedUser _loggedUser;
        private readonly IPlansReadOnlyRepository _plansReadOnlyRepository;
        private readonly IUserReadOnlyRepository _userReadOnlyRepository;
        private readonly IUserUpdateOnlyRepository _userUpdateOnlyRepository;

        public CreateSubscriptionCheckoutUseCase (
            IUnitOfWork unitOfWork,
            IPaymentGateway paymentGateway,
            ILoggedUser loggedUser,
            IPlansReadOnlyRepository plansReadOnlyRepository,
            IUserReadOnlyRepository userReadOnlyRepository,
            IUserUpdateOnlyRepository userUpdateOnlyRepository)
        {
            _unitOfWork = unitOfWork;
            _paymentGateway = paymentGateway;
            _loggedUser = loggedUser;
            _plansReadOnlyRepository = plansReadOnlyRepository;
            _userReadOnlyRepository = userReadOnlyRepository;
            _userUpdateOnlyRepository = userUpdateOnlyRepository;

        }
        public async Task<ResponseSubscriptionCheckoutJson> ExecuteAsync ( 
            RequestCreateSubscriptionCheckoutJson request , CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();

            var plan =
                await _plansReadOnlyRepository.GetByIdAsync(
                    request.PlanId ,
                    cancellationToken);

            if ( plan is null ) {
                throw new NotFoundException(
                    ResourceErrorMessages.PLAN_NOT_FOUND);
            }

            if ( string.IsNullOrWhiteSpace(loggedUser.PagarMeCustomerId) ) {

                loggedUser.PagarMeCustomerId =
                    await _paymentGateway.CreateCustomerAsync(
                        loggedUser ,
                        cancellationToken);

                _userUpdateOnlyRepository.Update(loggedUser);

                await _unitOfWork.Commit(cancellationToken);
            }

            var checkout =
                await _paymentGateway.CreateSubscriptionCheckoutAsync(
                    loggedUser ,
                    plan ,
                    cancellationToken);

            return new ResponseSubscriptionCheckoutJson
            {
                CheckoutUrl = checkout.CheckoutUrl
            };
        }
    }
}
