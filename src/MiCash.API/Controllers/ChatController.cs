using MiCash.Application.UseCases.FinancialChat;
using MiCash.Communication.Requests.Chat;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiCash.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        [HttpPost("message")]
        [ProducesResponseType(typeof(ResponseChatMessageJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Chat (
            [FromServices] IFinancialChatUseCase useCase ,
            [FromBody] RequestChatMessageJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.SendChatMessageAsync(request , cancellationToken);

            return Ok(response);
        }
    }
}
