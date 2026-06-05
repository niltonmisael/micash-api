using MiCash.Application.UseCases.Subscriptions.Create;
using MiCash.Application.UseCases.Subscriptions.CreateCheckout;
using MiCash.Application.UseCases.Subscriptions.GetCurrent;
using MiCash.Communication.Requests.Subscriptions;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiCash.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateSubscriptions (
            [FromServices] ICreateSubscriptionUseCase useCase,
            [FromBody] RequestCreateSubscriptionJson request,
            CancellationToken cancellationToken)
        {
            await useCase.ExecuteAsync(request, cancellationToken);

            return Created(string.Empty, null);
        }

        [HttpGet("current")]
        [Authorize]
        [ProducesResponseType(typeof(ResponseCurrentSubscriptionJson), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrentSubscription (
            [FromServices] IGetCurrentSubscriptionUseCase useCase ,
            [FromBody] RequestCreateSubscriptionJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.ExecuteAsync(cancellationToken);

            if (response is null ) {

                return Ok(new ResponseCurrentSubscriptionJson
                {
                    PlanName = "Free" ,
                    Status = "Free"
                });
            }

            return Ok(response);
        }

        [HttpPost("checkout")]
        [Authorize]
        [ProducesResponseType(typeof(ResponseSubscriptionCheckoutJson) , StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateCheckout (
            [FromServices] ICreateSubscriptionCheckoutUseCase useCase ,
            [FromBody] RequestCreateSubscriptionCheckoutJson request ,
            CancellationToken cancellationToken )
        {
            var response =
                await useCase.ExecuteAsync(
                    request ,
                    cancellationToken);

            return Ok(response);
        }
    }
}
