using MiCash.API.Attributes;
using MiCash.Communication.Responses;
using MiCash.Domain.Security.Tokens;
using MiCash.Exception;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MiCash.API.Filters
{
    /// <summary>
    /// Global: a signed-in user whose e-mail is not confirmed gets 403 with errorCode EMAIL_NOT_VERIFIED on every
    /// endpoint without [AllowUnverifiedEmail]; the app then opens the code screen. Reads the access token claim,
    /// so it costs no database query. Anonymous requests (login, register, webhooks…) are not affected.
    /// </summary>
    public sealed class EmailVerifiedFilter : IAuthorizationFilter
    {
        public const string CODE = "EMAIL_NOT_VERIFIED";

        public void OnAuthorization ( AuthorizationFilterContext context )
        {
            var user = context.HttpContext.User;

            if ( user.Identity?.IsAuthenticated != true ) {
                return;
            }

            var metadata = context.ActionDescriptor.EndpointMetadata;

            // Only endpoints that require a login. Anonymous ones (login, refresh, register, forgot-password…)
            // stay open even when the app sends the unverified token along.
            if ( metadata.OfType<IAuthorizeData>().Any() == false || metadata.OfType<IAllowAnonymous>().Any() ) {
                return;
            }

            if ( metadata.OfType<AllowUnverifiedEmailAttribute>().Any() ) {
                return;
            }

            // Missing claim = token issued before this feature, whose users were all marked as verified.
            if ( user.FindFirst(MiCashClaims.EMAIL_VERIFIED)?.Value != "false" ) {
                return;
            }

            context.Result = new ObjectResult(new ResponseErrorJson(ResourceErrorMessages.EMAIL_NOT_VERIFIED)
            {
                ErrorCode = CODE
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
