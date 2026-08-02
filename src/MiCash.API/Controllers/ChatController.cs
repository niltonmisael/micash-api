using MiCash.Application.UseCases.FinancialChat;
using MiCash.Communication.Requests.Chat;
using MiCash.Communication.Responses;
using Microsoft.AspNetCore.Mvc;

namespace MiCash.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        [HttpPost("messagens")]
        [ProducesResponseType(typeof(ResponseRegisteredExpenseJson) , StatusCodes.Status201Created)]
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
