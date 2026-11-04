using MiCash.Application.UseCases.Subscriptions.Cancel;
using MiCash.Application.UseCases.Subscriptions.GetCurrent;
using MiCash.Application.UseCases.Subscriptions.History;
using MiCash.Application.UseCases.Subscriptions.Limits;
using MiCash.Application.UseCases.Subscriptions.Reactivate;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.Checkout;
using MiCash.Communication.Responses.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiCash.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {

        [Authorize]
        [HttpGet("current")]
        [ProducesResponseType(typeof(ResponseCurrentSubscriptionJson), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrentSubscription (
            [FromServices] IGetCurrentSubscriptionUseCase useCase,
            CancellationToken cancellationToken )
        {
            var response = await useCase.ExecuteAsync(cancellationToken);

            return Ok(response);
        }

        /// <summary>
        /// Limits of the user's plan (Free or Premium) and chat usage, so the app can show "3 de 5 mensagens"
        /// and the subscription offers before the user hits a 403 PLAN_LIMIT_REACHED.
        /// </summary>
        [Authorize]
        [HttpGet("limits")]
        [ProducesResponseType(typeof(ResponsePlanLimitsJson) , StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLimits (
            [FromServices] IGetPlanLimitsUseCase useCase ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(cancellationToken);

            return Ok(response);
        }

        [Authorize]
        [HttpDelete("cancel")]
        [ProducesResponseType(typeof(ResponseCheckoutJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelSubscription (
            [FromServices] ICancelSubscriptionUseCase useCase ,
            CancellationToken cancellationToken )
        {
            await useCase.ExecuteAsync(cancellationToken);

            return Ok();
        }

        [Authorize]
        [HttpPost("reactivate")]
        [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ReactivateSubscription (
            [FromServices] IReactivateSubscriptionUseCase useCase ,
            CancellationToken cancellationToken )
        {
            await useCase.ExecuteAsync(
                cancellationToken);

            return NoContent();
        }

        [Authorize]
        [HttpGet("history")]
        [ProducesResponseType(typeof(ResponseSubscriptionHistoryJson) , StatusCodes.Status200OK)]
        public async Task<IActionResult> HistorySubscription (
            [FromServices] ISubscriptionHistoryUseCase useCase ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.ExecuteAsync(cancellationToken);

            return Ok(response);
        }

    }
}
