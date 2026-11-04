using MiCash.Communication.Responses.Subscriptions;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Domain.Services.Plans;

namespace MiCash.Application.UseCases.Subscriptions.GetCurrent
{
    /// <summary>
    /// GET api/subscription/current. The subscription that gives access (Active, or refused renewal in grace) wins;
    /// otherwise a checkout still Pending; otherwise Free. Premium is decided by <see cref="SubscriptionAccess"/>.
    /// </summary>
    public class GetCurrentSubscriptionUseCase : IGetCurrentSubscriptionUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly TimeProvider _time;

        public GetCurrentSubscriptionUseCase (
            ILoggedUser loggedUser ,
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository ,
            TimeProvider time )
        {
            _loggedUser = loggedUser;
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _time = time;
        }

        public async Task<ResponseCurrentSubscriptionJson> ExecuteAsync (
            CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var subscription =
                await _subscriptionsReadOnlyRepository.GetWithAccessByUserIdAsync(user.Id , cancellationToken)
                ?? await _subscriptionsReadOnlyRepository.GetCurrentByUserIdAsync(user.Id , cancellationToken);

            if ( subscription is null ) {
                return new ResponseCurrentSubscriptionJson
                {
                    PlanName = "Free" ,
                    Status = 0 ,
                    StatusDescription = "Free"
                };
            }

            var now = _time.GetUtcNow().UtcDateTime;
            var isPremium = SubscriptionAccess.HasPremium(subscription , now);
            var inGrace = SubscriptionAccess.InGracePeriod(subscription , now);

            return new ResponseCurrentSubscriptionJson
            {
                PlanId = subscription.PlanId ,
                PlanName = subscription.Plan.Name ,
                Status = ( int ) subscription.Status ,
                StatusDescription = subscription.Status.ToString() ,
                IsPremium = isPremium ,
                StartsAt = subscription.StartsAt ,
                CancelAtPeriodEnd = subscription.CancelAtPeriodEnd ,
                AutoRenew = subscription.Status == Domain.Enums.SubscriptionStatus.Active && subscription.CancelAtPeriodEnd == false ,
                NextBillingDate = subscription.NextBillingDate ,
                AccessEndsAt = AccessEndsAt(subscription , isPremium , inGrace) ,
                PaymentGraceUntil = inGrace ? subscription.PaymentGraceUntil : null ,
                EndsAt = subscription.EndsAt ,
                CancellationRequestedAt = subscription.CancellationRequestedAt ,
                CancellationScheduledFor = subscription.CancellationScheduledFor ,
                Price = subscription.Plan.Price ,
                BillingPeriod = subscription.Plan.BillingPeriod.ToString()
            };
        }

        private static DateTime? AccessEndsAt ( Subscription subscription , bool isPremium , bool inGrace )
        {
            if ( isPremium == false ) {
                return null;
            }

            if ( inGrace ) {
                return subscription.PaymentGraceUntil;
            }

            return subscription.CancelAtPeriodEnd
                ? subscription.CancellationScheduledFor ?? subscription.EndsAt
                : null;
        }
    }
}
