using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.Jobs;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Subscriptions.Cancel
{
    public class CancelSubscriptionUseCase : ICancelSubscriptionUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly ISubscriptionsUpdateOnlyRepository _subscriptionsUpdateOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBackgroundJobService _backgroundJobService;

        public CancelSubscriptionUseCase (
            ILoggedUser loggedUser ,
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository ,
            ISubscriptionsUpdateOnlyRepository subscriptionsUpdateOnlyRepository ,
            IUnitOfWork unitOfWork,
            IBackgroundJobService backgroundJobService)
        {
            _loggedUser = loggedUser;
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _subscriptionsUpdateOnlyRepository = subscriptionsUpdateOnlyRepository;
            _unitOfWork = unitOfWork;
            _backgroundJobService = backgroundJobService;
        }

        public async Task ExecuteAsync (
            CancellationToken cancellationToken )
        {
            var user =
                await _loggedUser.Get();

            var subscription =
                await _subscriptionsReadOnlyRepository
                    .GetActiveByUserIdAsync(
                        user.Id ,
                        cancellationToken);

            if ( subscription is null ) {
                throw new NotFoundException(ResourceErrorMessages.SUBSCRIPTION_NOT_FOUND);
            }

            if ( subscription.CancelAtPeriodEnd ) {
                throw new BadRequestException([ResourceErrorMessages.SUBSCRIPTION_ALREADY_HAS_A_SCHEDULED_CANCELLATION]);
            }

            var executionDate = subscription.NextBillingDate!.Value;

            var jobId = _backgroundJobService.ScheduleSubscriptionCancellation(subscription.Id , executionDate);

            subscription.CancelAtPeriodEnd = true;

            subscription.CancellationRequestedAt =
                DateTime.UtcNow;

            subscription.UpdatedAt =
                DateTime.UtcNow;

            subscription.CancellationJobId = jobId;

            subscription.CancellationScheduledFor = executionDate;

            _subscriptionsUpdateOnlyRepository
                .Update(subscription ,cancellationToken);

            await _unitOfWork.Commit(cancellationToken);
        }
    }
}
