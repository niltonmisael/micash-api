using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Services.LoggedUser;
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
            var user = await _loggedUser.Get();

            var subscription =
                await _subscriptionsReadOnlyRepository
                    .GetCurrentByUserIdAsync(
                        user.Id ,
                        context.HttpContext.RequestAborted);

            if ( subscription is null ) {
                context.Result =
                    new ObjectResult(new
                    {
                        Message = "Esta funcionalidade requer uma assinatura ativa."
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };

                return;
            }

            if ( subscription.Status != SubscriptionStatus.Active ) {
                context.Result =
                    new ObjectResult(new
                    {
                        Message = "Sua assinatura não está ativa."
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };

                return;
            }
        }
    }
}
