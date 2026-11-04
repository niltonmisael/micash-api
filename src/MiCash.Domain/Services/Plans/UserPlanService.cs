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
    /// Same rule as [RequireActiveSubscription]: only an Active subscription is Premium (Pending checkout,
    /// past due, cancelled… are Free). Scoped: the subscription is read once per request.
    /// </summary>
    public sealed class UserPlanService : IUserPlanService
    {
        private readonly ISubscriptionsReadOnlyRepository _subscriptions;
        private readonly Dictionary<long , PlanLimits> _byUser = [];

        public UserPlanService ( ISubscriptionsReadOnlyRepository subscriptions )
        {
            _subscriptions = subscriptions;
        }

        public async Task<PlanLimits> GetLimitsAsync ( User user , CancellationToken cancellationToken )
        {
            if ( _byUser.TryGetValue(user.Id , out var limits) ) {
                return limits;
            }

            var subscription = await _subscriptions.GetCurrentByUserIdAsync(user.Id , cancellationToken);

            limits = subscription?.Status == SubscriptionStatus.Active ? PlanLimits.Premium : PlanLimits.Free;

            _byUser[user.Id] = limits;

            return limits;
        }
    }
}
