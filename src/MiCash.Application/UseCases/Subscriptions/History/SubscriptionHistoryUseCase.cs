using MiCash.Communication.Responses.Subscriptions;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.Subscriptions.History
{
    /// <summary>
    /// Not cached: a user has only a handful of subscriptions, and webhooks/jobs change their status at any
    /// time — a cache would show a stale status (e.g. still "Pending" after the payment was confirmed).
    /// </summary>
    public class SubscriptionHistoryUseCase : ISubscriptionHistoryUseCase
    {
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly ILoggedUser _loggedUser;

        public SubscriptionHistoryUseCase (
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository,
            ILoggedUser loggedUser)
        {
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _loggedUser = loggedUser;
        }

        public async Task<IReadOnlyCollection<ResponseSubscriptionHistoryJson>>ExecuteAsync (CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var subscriptions = await _subscriptionsReadOnlyRepository.GetHistoryByUserIdAsync(user.Id ,cancellationToken);

            return subscriptions
                .Select(subscription =>
                    new ResponseSubscriptionHistoryJson
                    {
                        SubscriptionId = subscription.Id ,
                        PlanId = subscription.PlanId ,
                        PlanName = subscription.Plan.Name ,
                        Status = ( int ) subscription.Status ,
                        StatusDescription = subscription.Status.ToString() ,
                        StartsAt = subscription.StartsAt ,
                        EndsAt = subscription.EndsAt ,
                        CreatedAt = subscription.CreatedAt
                    })
                .ToList();
        }
    }
}
