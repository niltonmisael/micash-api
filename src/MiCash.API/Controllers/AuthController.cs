using MiCash.Application.UseCases.Auth.Login;
using MiCash.Application.UseCases.Login.Logout;
using MiCash.Application.UseCases.Login.Refresh;
using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using MiCash.API.RateLimiting;
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

        [HttpPost("/logout")]
        [Authorize]
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
