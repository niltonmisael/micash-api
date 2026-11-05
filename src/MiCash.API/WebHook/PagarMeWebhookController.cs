using MiCash.Application.UseCases.WebHook;
using MiCash.Domain.Security.WebHook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MiCash.API.WebHook
{
    [Route("api/pagar-me-webhook")]
    [ApiController]
    public class PagarMeWebhookController : ControllerBase
    {
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Receive (
            [FromServices] IProcessPagarMeWebhookUseCase useCase ,
            [FromServices] IWebhookAuthenticator authenticator ,
            [FromBody] JsonElement payload ,
            CancellationToken cancellationToken )
        {
            var authorization =
                Request.Headers.Authorization.ToString();

            if ( !authenticator.IsValid(authorization) ) {
                return Unauthorized();
            }

            await useCase.ExecuteAsync(
                payload ,
                cancellationToken);

            return Ok();
        }
    }
}
