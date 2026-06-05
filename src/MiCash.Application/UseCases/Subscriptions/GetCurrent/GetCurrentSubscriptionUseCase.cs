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
            ILoggedUser loggedUser,
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository)
        {
            _loggedUser = loggedUser;
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
        }
        public async Task<ResponseCurrentSubscriptionJson> ExecuteAsync ( CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();

            var subscription =
                await _subscriptionsReadOnlyRepository.GetCurrentByUserIdAsync(loggedUser.Id, cancellationToken);

            if ( subscription is null ) {
                return null;
            }

            return new ResponseCurrentSubscriptionJson
            {
                Id = subscription.Id ,
                PlanName = subscription.Plan.Name ,
                Price = subscription.Plan.Price ,
                Status = subscription.Status.ToString() ,
                NextBillingDate = subscription.NextBillingDate
            };
        }
    }
}
