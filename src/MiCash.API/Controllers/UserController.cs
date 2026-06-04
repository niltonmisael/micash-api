using MiCash.Application.UseCases.Users.ChangePassword;
using MiCash.Application.UseCases.Users.Delete;
using MiCash.Application.UseCases.Users.Profile;
using MiCash.Application.UseCases.Users.Register;
using MiCash.Application.UseCases.Users.Update;
using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiCash.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {

        [HttpPost("register")]
        [ProducesResponseType(typeof(ResponseRegisteredUserJson), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
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

        [HttpDelete("delete")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteProfile ( 
            [FromServices] IDeleteUserAccountUseCase useCase,
            CancellationToken cancellationToken)
        {
            await useCase.Execute(cancellationToken);

            return NoContent();
        }
    }
}
