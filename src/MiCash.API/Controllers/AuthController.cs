using MiCash.Application.UseCases.Auth.Login;
using MiCash.Application.UseCases.Login.Logout;
using MiCash.Application.UseCases.Login.Refresh;
using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
