using MiCash.Application.UseCases.Notifications;
using MiCash.Communication.Requests.Notifications;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MiCash.API.Controllers
{
    [Route("api/notification")]
    [SwaggerTag("Central de notificações do app (sino). Toda notificação gerada para o usuário fica aqui, tenha ele aparelho ativo ou não.")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        [HttpGet]
        [SwaggerOperation(
            Summary = "Listar notificações" ,
            Description = "Da mais recente para a mais antiga, paginadas por cursor: para carregar mais, envie before = id da última notificação recebida. limit de 1 a 50 (padrão 20). 204 quando não há notificações."
        )]
        [ProducesResponseType(typeof(ResponseNotificationsJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetNotifications (
            [FromServices] IGetNotificationsUseCase useCase ,
            [FromQuery] RequestNotificationsJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(request , cancellationToken);

            if ( response.Notifications.Count == 0 ) {
                return NoContent();
            }

            return Ok(response);
        }

        [HttpGet("unread-count")]
        [SwaggerOperation(Summary = "Quantidade de notificações não lidas (número do sino)")]
        [ProducesResponseType(typeof(ResponseUnreadNotificationsCountJson) , StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnreadCount (
            [FromServices] IGetUnreadNotificationsCountUseCase useCase ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(cancellationToken);

            return Ok(response);
        }

        [HttpPut("{id:long}/read")]
        [SwaggerOperation(
            Summary = "Marcar uma notificação como lida" ,
            Description = "Idempotente: se já estiver lida, mantém o horário original. Notificação de outro usuário ou inexistente: 404."
        )]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead (
            [FromServices] IMarkNotificationAsReadUseCase useCase ,
            [FromRoute] long id ,
            CancellationToken cancellationToken )
        {
            await useCase.Execute(id , cancellationToken);

            return NoContent();
        }

        [HttpPut("read-all")]
        [SwaggerOperation(Summary = "Marcar todas as notificações como lidas")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> MarkAllAsRead (
            [FromServices] IMarkAllNotificationsAsReadUseCase useCase ,
            CancellationToken cancellationToken )
        {
            await useCase.Execute(cancellationToken);

            return NoContent();
        }
    }
}
