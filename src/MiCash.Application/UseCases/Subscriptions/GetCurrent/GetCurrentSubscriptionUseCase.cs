using MiCash.Communication.Responses.Subscriptions;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Subscriptions.GetCurrent
{
    public class GetCurrentSubscriptionUseCase : IGetCurrentSubscriptionUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;

        public GetCurrentSubscriptionUseCase (
            ILoggedUser loggedUser ,
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository )
        {
            _loggedUser = loggedUser;
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
        }

        public async Task<ResponseCurrentSubscriptionJson> ExecuteAsync (
            CancellationToken cancellationToken )
        {
            var user =
                await _loggedUser.Get();

            var subscription =
                await _subscriptionsReadOnlyRepository
                    .GetCurrentByUserIdAsync(
                        user.Id ,
                        cancellationToken);

            if ( subscription is null ) {
                return new ResponseCurrentSubscriptionJson
                {
                    PlanName = "Free" ,
                    Status = 0 ,
                    StatusDescription = "Free"
                };
            }

            return new ResponseCurrentSubscriptionJson
            {
                PlanId = subscription.PlanId ,

                PlanName =
                    subscription.Plan.Name ,

                Status =
                    ( int ) subscription.Status ,

                StatusDescription =
                    subscription.Status.ToString() ,

                CancelAtPeriodEnd =
                    subscription.CancelAtPeriodEnd ,

                NextBillingDate =
                    subscription.NextBillingDate ,

                EndsAt =
                    subscription.EndsAt ,

                CancellationRequestedAt =
                    subscription.CancellationRequestedAt ,

                CancellationScheduledFor =
                    subscription.CancellationScheduledFor ,

                Price =
                    subscription.Plan.Price
            };
        }
    }
}
