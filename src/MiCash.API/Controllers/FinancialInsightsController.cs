using MiCash.Application.UseCases.FinancialInsights.Get;
using MiCash.Communication.Requests.FinancialInsights;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.FinancialInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiCash.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FinancialInsightsController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(
            typeof(ResponseFinancialInsightsJson) ,
            StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Get (
            [FromServices] IGetFinancialInsightsUseCase useCase ,
            [FromQuery] RequestFinancialInsightJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(
                request,
                cancellationToken);

            return Ok(response);
        }
    }
}
