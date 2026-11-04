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
        /// <summary>How long before the next billing date the cancellation is sent to Pagar.me.</summary>
        public static readonly TimeSpan CancellationLeadTime = TimeSpan.FromHours(6);

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

            if ( subscription.NextBillingDate is not { } nextBillingDate ) {
                throw new BadRequestException([ResourceErrorMessages.SUBSCRIPTION_BILLING_DATE_UNAVAILABLE]);
            }

            var now = DateTime.UtcNow;

            // Charges are stopped at Pagar.me a little before the billing date, so the next cycle is not charged
            // first. The access itself ends only at the billing date (SubscriptionExpirationJob).
            var executionDate = nextBillingDate - CancellationLeadTime;

            if ( executionDate < now ) {
                executionDate = now;
            }

            var jobId = _backgroundJobService.ScheduleSubscriptionCancellation(subscription.Id , executionDate);

            subscription.CancelAtPeriodEnd = true;

            subscription.CancellationRequestedAt =
                now;

            subscription.UpdatedAt =
                now;

            subscription.CancellationJobId = jobId;

            // What the user sees: Premium is kept until the end of the period already paid.
            subscription.CancellationScheduledFor = nextBillingDate;

            _subscriptionsUpdateOnlyRepository
                .Update(subscription ,cancellationToken);

            await _unitOfWork.Commit(cancellationToken);
        }
    }
}
