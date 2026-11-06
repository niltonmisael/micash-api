using MiCash.API.Attributes;
using MiCash.API.RateLimiting;
using MiCash.Application.UseCases.Auth.EmailVerification;
using MiCash.Application.UseCases.Users.ChangePassword;
using MiCash.Communication.Requests.EmailVerification;
using MiCash.Communication.Responses.EmailVerification;
using MiCash.Application.UseCases.Users.Delete;
using MiCash.Application.UseCases.Users.Profile;
using MiCash.Application.UseCases.Users.Register;
using MiCash.Application.UseCases.Users.NotificationPreferences;
using MiCash.Application.UseCases.Users.Update;
using MiCash.Communication.Requests.Users;
using MiCash.Communication.Responses.Users;
using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MiCash.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {

        /// <summary>
        /// Creates the account NOT verified (emailVerified: false) and e-mails a 6-digit code. The tokens returned
        /// only reach the verification endpoints until POST /verify-email.
        /// </summary>
        [HttpPost("register")]
        [EnableRateLimiting(PasswordResetRateLimit.EMAIL_VERIFICATION_POLICY)]
        [ProducesResponseType(typeof(ResponseRegisteredUserJson), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Register (
            [FromServices] IRegisterUserUseCase useCase,  
            [FromBody] RequestRegisterUserJson request,
            CancellationToken cancellationToken)
        {
            var response = await useCase.Execute(request, cancellationToken);

            return Created(string.Empty, response);
            
        }

        [HttpGet("profile")]
        [ProducesResponseType(typeof(ResponseUserProfileJson) , StatusCodes.Status200OK)]
        [Authorize]
        [AllowUnverifiedEmail]
        public async Task<IActionResult> GetProfile ( [FromServices] IGetUserProfileUseCase useCase)
        {
            var response = await useCase.Execute();

            return Ok(response);
        }

        [HttpPut("update")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProfile ( 
            [FromServices] IUpdateUserUseCase useCase, 
            [FromBody] RequestUpdateUserJson request, 
            CancellationToken cancellationToken )
        {
            await useCase.Execute(request, cancellationToken);

            return NoContent();
        }

        /// <summary>
        /// E-mail change, step 1: checks the current password and sends a 6-digit code to the new address.
        /// The account keeps the current e-mail until step 2. Call again (after 60s) to resend.
        /// </summary>
        [HttpPost("email-change")]
        [Authorize]
        [EnableRateLimiting(PasswordResetRateLimit.EMAIL_VERIFICATION_POLICY)]
        [ProducesResponseType(typeof(ResponseEmailVerificationCodeSentJson) , StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> RequestEmailChange (
            [FromServices] IRequestEmailChangeUseCase useCase ,
            [FromBody] RequestEmailChangeJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(request , cancellationToken);

            return Accepted(response);
        }

        /// <summary>E-mail change, step 2: the code received at the new address applies the change.</summary>
        [HttpPost("email-change/confirm")]
        [Authorize]
        [EnableRateLimiting(PasswordResetRateLimit.EMAIL_VERIFICATION_POLICY)]
        [ProducesResponseType(typeof(ResponseUserProfileJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ConfirmEmailChange (
            [FromServices] IConfirmEmailChangeUseCase useCase ,
            [FromBody] RequestConfirmEmailChangeJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(request , cancellationToken);

            return Ok(response);
        }

        /// <summary>Monthly summary (e-mail and push) and news e-mail switches.</summary>
        [HttpGet("notification-preferences")]
        [Authorize]
        [ProducesResponseType(typeof(ResponseNotificationPreferencesJson) , StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotificationPreferences ( [FromServices] IGetNotificationPreferencesUseCase useCase )
        {
            return Ok(await useCase.Execute());
        }

        [HttpPut("notification-preferences")]
        [Authorize]
        [ProducesResponseType(typeof(ResponseNotificationPreferencesJson) , StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateNotificationPreferences (
            [FromServices] IUpdateNotificationPreferencesUseCase useCase ,
            [FromBody] RequestNotificationPreferencesJson request ,
            CancellationToken cancellationToken )
        {
            return Ok(await useCase.Execute(request , cancellationToken));
        }

        [HttpPut("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword ( 
            [FromServices] IChangePasswordUseCase useCase , 
            [FromBody] RequestChangePasswordJson request,
            CancellationToken cancellationToken)
        {
            await useCase.Execute(request, cancellationToken);

            return NoContent();
        }

        /// <summary>
        /// Deletes the account. Requires the current password; a subscription still billing is canceled at Pagar.me
        /// first (503 if Pagar.me fails, and nothing is deleted).
        /// </summary>
        [HttpDelete("delete")]
        [Authorize]
        [AllowUnverifiedEmail]
        // Same per-IP limit as the login: the password check must not become a way to guess it.
        [EnableRateLimiting(PasswordResetRateLimit.LOGIN_POLICY)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> DeleteProfile (
            [FromServices] IDeleteUserAccountUseCase useCase,
            [FromBody] RequestDeleteAccountJson request,
            CancellationToken cancellationToken)
        {
            await useCase.Execute(request , cancellationToken);

            return NoContent();
        }
    }
}
