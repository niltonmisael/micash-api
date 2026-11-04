using MiCash.API.Attributes;
using MiCash.Application.UseCases.FinancialChat;
using MiCash.Application.UseCases.FinancialChat.History;
using MiCash.Application.UseCases.FinancialChat.StartConversation;
using MiCash.Communication.Requests.Chat;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MiCash.API.Controllers
{
    [Route("api/[controller]")]
    [SwaggerTag("Chat com a MI, a assistente financeira (recurso para assinantes).")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        [HttpPost("message")]
        [SwaggerOperation(
            Summary = "Enviar mensagem para a MI" ,
            Description = "Envia uma mensagem na conversa ativa (criada automaticamente) e retorna a resposta da MI. As mensagens só são gravadas se a IA responder."
        )]
        [ProducesResponseType(typeof(ResponseChatMessageJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status502BadGateway)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Chat (
            [FromServices] IFinancialChatUseCase useCase ,
            [FromBody] RequestChatMessageJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.SendChatMessageAsync(request , cancellationToken);

            return Ok(response);
        }

        [HttpGet("messages")]
        [SwaggerOperation(
            Summary = "Histórico da conversa ativa" ,
            Description = "Retorna as mensagens da conversa ativa em ordem cronológica, paginadas por cursor: para carregar mensagens mais antigas, envie before = id da mensagem mais antiga recebida. Não exige assinatura ativa (o usuário pode reler suas conversas)."
        )]
        [ProducesResponseType(typeof(ResponseChatHistoryJson) , StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMessages (
            [FromServices] IGetChatHistoryUseCase useCase ,
            [FromQuery] RequestChatHistoryJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(request , cancellationToken);

            return Ok(response);
        }

        [HttpPost("conversations")]
        [SwaggerOperation(
            Summary = "Iniciar nova conversa" ,
            Description = "Encerra a conversa ativa (as mensagens ficam guardadas) e abre uma nova, vazia. A MI deixa de considerar o histórico anterior."
        )]
        [ProducesResponseType(typeof(ResponseStartedConversationJson) , StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status409Conflict)]
        public async Task<IActionResult> StartConversation (
            [FromServices] IStartConversationUseCase useCase ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(cancellationToken);

            return Created(string.Empty , response);
        }
    }
}
