using MiCash.Communication.Responses.Subscriptions;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.LoggedUser;
using System.Text.Json;

namespace MiCash.Application.UseCases.Subscriptions.History
{
    public class SubscriptionHistoryUseCase : ISubscriptionHistoryUseCase
    {
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly ILoggedUser _loggedUser;
        private readonly IRedisCacheService _redisCacheService;

        public SubscriptionHistoryUseCase (
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository,
            ILoggedUser loggedUser,
            IRedisCacheService redisCacheService)
        {
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _loggedUser = loggedUser;
            _redisCacheService = redisCacheService;
        }
        
        public async Task<IReadOnlyCollection<ResponseSubscriptionHistoryJson>>ExecuteAsync (CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var subscriptions = await _subscriptionsReadOnlyRepository.GetHistoryByUserIdAsync(user.Id ,cancellationToken);

            var cacheKey = CacheKeys.SubscriptionHistory(user.Id);

            var cached = await _redisCacheService.GetAsync<string>(cacheKey);

            if(!string.IsNullOrWhiteSpace(cached)) {

                var cachedResponse = JsonSerializer.Deserialize<List<ResponseSubscriptionHistoryJson>>(cached);

                if ( cachedResponse is not null ) {
                    return cachedResponse;
                }
            }

            var response = subscriptions
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

            await _redisCacheService.SetAsync(
                cacheKey ,
                JsonSerializer.Serialize(response) ,
                TimeSpan.FromMinutes(30));

            return response;

        }
    }
    
}
