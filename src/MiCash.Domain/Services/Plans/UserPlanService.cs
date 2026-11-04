using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.Subscriptions;

namespace MiCash.Domain.Services.Plans
{
    public interface IUserPlanService
    {
        /// <summary>Limits of the user's plan: Premium with an Active subscription, Free otherwise.</summary>
        Task<PlanLimits> GetLimitsAsync ( User user , CancellationToken cancellationToken );
    }

    /// <summary>
    /// Premium or Free by <see cref="SubscriptionAccess.HasPremium"/> (same rule as [RequireActiveSubscription]).
    /// Only the server's records decide it; nothing the client sends matters. Scoped: read once per request.
    /// </summary>
    public sealed class UserPlanService : IUserPlanService
    {
        private readonly ISubscriptionsReadOnlyRepository _subscriptions;
        private readonly TimeProvider _time;
        private readonly Dictionary<long , PlanLimits> _byUser = [];

        public UserPlanService ( ISubscriptionsReadOnlyRepository subscriptions , TimeProvider time )
        {
            _subscriptions = subscriptions;
            _time = time;
        }

        public async Task<PlanLimits> GetLimitsAsync ( User user , CancellationToken cancellationToken )
        {
            if ( _byUser.TryGetValue(user.Id , out var limits) ) {
                return limits;
            }

            var subscription = await _subscriptions.GetWithAccessByUserIdAsync(user.Id , cancellationToken);

            limits = SubscriptionAccess.HasPremium(subscription , _time.GetUtcNow().UtcDateTime)
                ? PlanLimits.Premium
                : PlanLimits.Free;

            _byUser[user.Id] = limits;

            return limits;
        }
    }
}
