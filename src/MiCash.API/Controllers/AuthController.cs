using MiCash.Application.UseCases.Auth.Login;
using MiCash.Application.UseCases.Login.Logout;
using MiCash.Application.UseCases.Login.Refresh;
using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using MiCash.API.Attributes;
using MiCash.API.RateLimiting;
using MiCash.Application.UseCases.Auth.EmailVerification;
using MiCash.Communication.Requests.EmailVerification;
using MiCash.Communication.Responses.EmailVerification;
using MiCash.Application.UseCases.Auth.PasswordReset;
using MiCash.Communication.Requests.PasswordReset;
using MiCash.Communication.Responses.PasswordReset;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MiCash.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpPost("/login")]
        [ProducesResponseType(typeof(ResponseRegisteredUserJson), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login ( 
            [FromServices] IDoLoginUseCase useCase, 
            [FromBody] RequestLoginJson request)
        {
            var response = await useCase.Execute(request);
            
            return Ok(response);
        }

        [HttpPost("/refresh")]
        [ProducesResponseType(typeof(ResponseRegisteredUserJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken ( 
            [FromServices] IRefreshTokenUseCase useCase , 
            [FromBody] RequestRefreshTokenJson request )
        {
            var response = await useCase.Execute(request);

            return Ok(response);
        }

        /// <summary>Step 1: e-mails a code when the address has an account. Same 202 answer either way.</summary>
        [HttpPost("/forgot-password")]
        [EnableRateLimiting(PasswordResetRateLimit.POLICY)]
        [ProducesResponseType(typeof(ResponseForgotPasswordJson) , StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ForgotPassword (
            [FromServices] IRequestPasswordResetUseCase useCase ,
            [FromBody] RequestForgotPasswordJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(request , cancellationToken);

            return Accepted(response);
        }

        /// <summary>Step 2: checks the code and returns a single-use reset token (not a session).</summary>
        [HttpPost("/verify-password-reset-code")]
        [EnableRateLimiting(PasswordResetRateLimit.POLICY)]
        [ProducesResponseType(typeof(ResponsePasswordResetCodeVerifiedJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> VerifyPasswordResetCode (
            [FromServices] IVerifyPasswordResetCodeUseCase useCase ,
            [FromBody] RequestVerifyPasswordResetCodeJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(request , cancellationToken);

            return Ok(response);
        }

        /// <summary>Step 3: sets the new password with the reset token. The user then logs in.</summary>
        [HttpPost("/reset-password")]
        [EnableRateLimiting(PasswordResetRateLimit.POLICY)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ResetPassword (
            [FromServices] IResetPasswordUseCase useCase ,
            [FromBody] RequestResetPasswordJson request ,
            CancellationToken cancellationToken )
        {
            await useCase.Execute(request , cancellationToken);

            return NoContent();
        }

        /// <summary>
        /// Confirms the e-mail with the 6-digit code. Returns NEW tokens (replace the stored ones) and
        /// showPremiumOffer (web sign-up only: the web shows the Premium offer; the mobile apps ignore it).
        /// </summary>
        [HttpPost("/verify-email")]
        [Authorize]
        [AllowUnverifiedEmail]
        [EnableRateLimiting(PasswordResetRateLimit.EMAIL_VERIFICATION_POLICY)]
        [ProducesResponseType(typeof(ResponseEmailVerifiedJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> VerifyEmail (
            [FromServices] IVerifyEmailUseCase useCase ,
            [FromBody] RequestVerifyEmailJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(request , cancellationToken);

            return Ok(response);
        }

        /// <summary>New code to the account e-mail. 429 with retryAt inside the 60s interval or above 5 per hour.</summary>
        [HttpPost("/resend-email-verification-code")]
        [Authorize]
        [AllowUnverifiedEmail]
        [EnableRateLimiting(PasswordResetRateLimit.EMAIL_VERIFICATION_POLICY)]
        [ProducesResponseType(typeof(ResponseEmailVerificationCodeSentJson) , StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ResendEmailVerificationCode (
            [FromServices] IResendEmailVerificationCodeUseCase useCase ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(cancellationToken);

            return Accepted(response);
        }

        /// <summary>Corrects a mistyped e-mail before confirming it; a new code goes to the new address.</summary>
        [HttpPut("/email-verification/email")]
        [Authorize]
        [AllowUnverifiedEmail]
        [EnableRateLimiting(PasswordResetRateLimit.EMAIL_VERIFICATION_POLICY)]
        [ProducesResponseType(typeof(ResponseEmailVerificationCodeSentJson) , StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ChangeVerificationEmail (
            [FromServices] IChangeVerificationEmailUseCase useCase ,
            [FromBody] RequestChangeVerificationEmailJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(request , cancellationToken);

            return Accepted(response);
        }

        [HttpPost("/logout")]
        [Authorize]
        [AllowUnverifiedEmail]
        [ProducesResponseType(typeof(ResponseRegisteredUserJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout (
            [FromServices] ILogoutUseCase useCase ,
            [FromBody] RequestLogoutJson request )
        {
            await useCase.Execute(request);

            return NoContent();
        }
    }
}
