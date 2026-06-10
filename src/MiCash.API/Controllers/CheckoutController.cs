using MiCash.Application.UseCases.Checkout;
using MiCash.Communication.Requests.Checkout;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.Checkout;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiCash.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(ResponseCheckoutJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateCheckout (
            [FromServices] ICreateCheckoutUseCase useCase ,
            [FromBody] RequestCreateCheckoutJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.ExecuteAsync(request , cancellationToken);

            return Ok(response);
        }

    }
}
