using MiCash.Communication.Responses;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Domain.Services.Plans;
using MiCash.Exception;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MiCash.API.Filters
{
    /// <summary>Premium-only endpoints. Same rule as the plan limits: <see cref="SubscriptionAccess.HasPremium"/>.</summary>
    public class RequireActiveSubscriptionFilter : IAsyncAuthorizationFilter
    {
        private readonly ILoggedUser _loggedUser;
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;
        private readonly TimeProvider _time;

        public RequireActiveSubscriptionFilter (
            ILoggedUser loggedUser ,
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository ,
            TimeProvider time )
        {
            _loggedUser = loggedUser;
            _subscriptionsReadOnlyRepository = subscriptionsReadOnlyRepository;
            _time = time;
        }

        public async Task OnAuthorizationAsync ( AuthorizationFilterContext context )
        {
            // Unauthenticated requests are answered by [Authorize] (401); never resolve the user here.
            if ( context.HttpContext.User.Identity?.IsAuthenticated != true ) {
                return;
            }

            var user = await _loggedUser.Get();

            var subscription =
                await _subscriptionsReadOnlyRepository
                    .GetWithAccessByUserIdAsync(
                        user.Id ,
                        context.HttpContext.RequestAborted);

            if ( subscription is null ) {
                // A checkout still Pending keeps the previous answer ("not active"); no subscription at all = required.
                var pending = await _subscriptionsReadOnlyRepository.GetCurrentByUserIdAsync(user.Id , context.HttpContext.RequestAborted);

                context.Result = Forbidden(pending is null
                    ? ResourceErrorMessages.SUBSCRIPTION_REQUIRED
                    : ResourceErrorMessages.SUBSCRIPTION_NOT_ACTIVE);

                return;
            }

            if ( SubscriptionAccess.HasPremium(subscription , _time.GetUtcNow().UtcDateTime) == false ) {
                context.Result = Forbidden(ResourceErrorMessages.SUBSCRIPTION_NOT_ACTIVE);
            }
        }

        // Same body as every other API error: { "errorMessages": [ ... ] }
        private static ObjectResult Forbidden ( string message ) =>
            new(new ResponseErrorJson(message))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
    }
}
