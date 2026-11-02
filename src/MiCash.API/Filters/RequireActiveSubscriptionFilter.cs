using MiCash.Communication.Responses;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MiCash.API.Filters
{
    public class RequireActiveSubscriptionFilter : IAsyncAuthorizationFilter
    {
        private readonly ILoggedUser _loggedUser;
        private readonly ISubscriptionsReadOnlyRepository _subscriptionsReadOnlyRepository;

        public RequireActiveSubscriptionFilter (
            ILoggedUser loggedUser ,
            ISubscriptionsReadOnlyRepository subscriptionsReadOnlyRepository )
        {
            _loggedUser = loggedUser;
            _subscriptionsReadOnlyRepository =
                subscriptionsReadOnlyRepository;
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
                    .GetCurrentByUserIdAsync(
                        user.Id ,
                        context.HttpContext.RequestAborted);

            if ( subscription is null ) {
                context.Result = Forbidden(ResourceErrorMessages.SUBSCRIPTION_REQUIRED);

                return;
            }

            if ( subscription.Status != SubscriptionStatus.Active ) {
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
