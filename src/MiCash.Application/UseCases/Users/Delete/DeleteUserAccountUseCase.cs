using MiCash.Application.Services.Subscriptions;
using MiCash.Communication.Requests.Users;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Services.Jobs;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Domain.Services.Payments;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging;

namespace MiCash.Application.UseCases.Users.Delete
{
    /// <summary>
    /// Deletes the account and everything in it (the database cascades the dependent rows).
    /// 1. The current password confirms the request: an open session alone is not enough.
    /// 2. Any subscription still billing is canceled at Pagar.me FIRST. If Pagar.me fails, nothing is deleted
    ///    (503): a deleted account must never keep being charged. A paid period left is lost (no refund).
    /// 3. After the deletion, a notice goes to the account e-mail.
    /// Other devices: the refresh token stops working (404) and the access token gets 401 (LoggedUser).
    /// </summary>
    public class DeleteUserAccountUseCase : IDeleteUserAccountUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IUserWriteOnlyRepository _userWriteOnlyRepository;
        private readonly ISubscriptionsReadOnlyRepository _subscriptions;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IPasswordEncripter _passwordEncripter;
        private readonly IBackgroundJobService _jobs;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteUserAccountUseCase> _logger;

        public DeleteUserAccountUseCase (
            ILoggedUser loggedUser ,
            IUserWriteOnlyRepository userWriteOnlyRepository ,
            ISubscriptionsReadOnlyRepository subscriptions ,
            IPaymentGateway paymentGateway ,
            IPasswordEncripter passwordEncripter ,
            IBackgroundJobService jobs ,
            IUnitOfWork unitOfWork ,
            ILogger<DeleteUserAccountUseCase> logger )
        {
            _loggedUser = loggedUser;
            _userWriteOnlyRepository = userWriteOnlyRepository;
            _subscriptions = subscriptions;
            _paymentGateway = paymentGateway;
            _passwordEncripter = passwordEncripter;
            _jobs = jobs;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Execute ( RequestDeleteAccountJson request , CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            if ( string.IsNullOrEmpty(request.Password) || _passwordEncripter.Verify(request.Password , user.PasswordHash) == false ) {
                throw new ErrorOnValidationException([ResourceErrorMessages.PASSWORD_DIFFERENT_CURRENT_PASSWORD]);
            }

            var billing = (await _subscriptions.GetHistoryByUserIdAsync(user.Id , cancellationToken))
                .Where(IsStillBilling)
                .ToList();

            await CancelAtGatewayAsync(user , billing , cancellationToken);

            // Scheduled cancellation/expiration jobs would only find a deleted subscription.
            foreach ( var subscription in billing.Where(s => string.IsNullOrWhiteSpace(s.CancellationJobId) == false) ) {
                _jobs.Delete(subscription.CancellationJobId!);
            }

            await _userWriteOnlyRepository.Delete(user);

            await _unitOfWork.Commit(cancellationToken);

            _jobs.EnqueueAccountDeletedEmail(user.Id , user.Email , user.Name);

            _logger.LogInformation("User {UserIdentifier} deleted the account." , user.UserIdentifier);
        }

        /// <summary>Still able to charge the card at Pagar.me (charges not stopped yet).</summary>
        private static bool IsStillBilling ( Subscription subscription ) =>
            string.IsNullOrWhiteSpace(subscription.PagarmeSubscriptionId) == false
            && subscription.Status is not (SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
            && SubscriptionCancellationRules.ChargesStopped(subscription) == false;

        private async Task CancelAtGatewayAsync ( User user , List<Subscription> billing , CancellationToken cancellationToken )
        {
            foreach ( var subscription in billing ) {
                try {
                    // Idempotent at the gateway: a retry after a partial failure is safe.
                    await _paymentGateway.CancelSubscriptionAsync(subscription.PagarmeSubscriptionId! , cancellationToken);
                } catch ( System.Exception ex ) when ( ex is not OperationCanceledException ) {
                    _logger.LogError(ex ,
                        "Account deletion of user {UserIdentifier} stopped: subscription {SubscriptionId} could not be canceled at Pagar.me." ,
                        user.UserIdentifier , subscription.Id);

                    throw new ServiceUnavailableException(ResourceErrorMessages.ACCOUNT_DELETION_SUBSCRIPTION_CANCEL_FAILED);
                }
            }
        }
    }
}
