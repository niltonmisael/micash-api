using MiCash.Application.UseCases.PushDevices;
using MiCash.Communication.Requests.PushDevices;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.PushDevices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MiCash.API.Controllers
{
    [Route("api/device")]
    [SwaggerTag("Aparelhos que recebem notificações push (Expo Push Service). O usuário vem sempre do token de acesso.")]
    [ApiController]
    [Authorize]
    public class DeviceController : ControllerBase
    {
        [HttpPost("push-token")]
        [SwaggerOperation(
            Summary = "Registrar o Expo Push Token deste aparelho" ,
            Description = "Cria ou atualiza (pelo token) e reativa o aparelho. Um token registrado por outra conta passa para a conta atual."
        )]
        [ProducesResponseType(typeof(ResponseRegisteredPushDeviceJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterPushToken (
            [FromServices] IRegisterPushDeviceUseCase useCase ,
            [FromBody] RequestRegisterPushDeviceJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(request , cancellationToken);

            return Ok(response);
        }

        [HttpDelete("push-token")]
        [SwaggerOperation(
            Summary = "Desativar o Expo Push Token deste aparelho" ,
            Description = "Chamar antes do logout. Token de outra conta ou inexistente: 404."
        )]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivatePushToken (
            [FromServices] IDeactivatePushDeviceUseCase useCase ,
            [FromBody] RequestDeactivatePushDeviceJson request ,
            CancellationToken cancellationToken )
        {
            await useCase.Execute(request , cancellationToken);

            return NoContent();
        }

        [HttpGet("push-tokens")]
        [SwaggerOperation(
            Summary = "Listar os aparelhos do usuário" ,
            Description = "Ativos primeiro. O token não é devolvido, só os últimos caracteres."
        )]
        [ProducesResponseType(typeof(ResponsePushDevicesJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetPushTokens (
            [FromServices] IGetPushDevicesUseCase useCase ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(cancellationToken);

            if ( response.Devices.Count == 0 ) {
                return NoContent();
            }

            return Ok(response);
        }

        [HttpPost("push-token/test")]
        [SwaggerOperation(
            Summary = "Enviar notificação de teste (somente Development)" ,
            Description = "Envia para os aparelhos ativos do próprio usuário. Fora de Development responde 404."
        )]
        [ProducesResponseType(typeof(ResponseTestPushNotificationJson) , StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendTestNotification (
            [FromServices] ISendTestPushNotificationUseCase useCase ,
            [FromServices] IWebHostEnvironment environment ,
            CancellationToken cancellationToken )
        {
            if ( environment.IsDevelopment() == false && environment.IsEnvironment("Test") == false ) {
                return NotFound();
            }

            var response = await useCase.Execute(cancellationToken);

            return Accepted(response);
        }
    }
}
