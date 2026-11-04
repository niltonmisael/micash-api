using MiCash.Application.Services.Subscriptions;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.Jobs;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Subscriptions.Reactivate
{
    public class ReactivateSubscriptionUseCase : IReactivateSubscriptionUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly ISubscriptionsUpdateOnlyRepository _subscriptionsUpdateOnlyRepository;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IUnitOfWork _unitOfWork;

        public ReactivateSubscriptionUseCase (
            ILoggedUser loggedUser ,
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository ,
            ISubscriptionsUpdateOnlyRepository subscriptionsUpdateOnlyRepository ,
            IBackgroundJobService backgroundJobService ,
            IUnitOfWork unitOfWork )
        {
            _loggedUser = loggedUser;
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _subscriptionsUpdateOnlyRepository = subscriptionsUpdateOnlyRepository;
            _backgroundJobService = backgroundJobService;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync (
            CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var subscription = await _subscriptionsReadOnlyRepository.GetActiveByUserIdAsync(user.Id ,cancellationToken);

            if ( subscription is null ) {
                throw new NotFoundException(ResourceErrorMessages.SUBSCRIPTION_NOT_FOUND);
            }

            if ( !subscription.CancelAtPeriodEnd ) {
                throw new BadRequestException([ResourceErrorMessages.THIS_SUBSCRIPTION_DOES_NOT_HAVE_A_SCHEDULED_CANCELLATION]);
            }

            // Already canceled at Pagar.me (last hours of the period): a canceled gateway subscription cannot be
            // resumed. The user keeps Premium until the period ends and can subscribe again afterwards.
            if ( SubscriptionCancellationRules.ChargesStopped(subscription) ) {
                throw new BadRequestException([ResourceErrorMessages.SUBSCRIPTION_CANCELLATION_ALREADY_PROCESSED]);
            }

            if ( !string.IsNullOrWhiteSpace(subscription.CancellationJobId) ) {

                _backgroundJobService.Delete(subscription.CancellationJobId);
            }

            subscription.CancelAtPeriodEnd = false;

            subscription.CancellationRequestedAt = null;

            subscription.CancellationScheduledFor = null;

            subscription.CancellationJobId = null;

            subscription.UpdatedAt = DateTime.UtcNow;

            _subscriptionsUpdateOnlyRepository.Update(subscription ,cancellationToken);

            await _unitOfWork.Commit(cancellationToken);
        }
    }
}
