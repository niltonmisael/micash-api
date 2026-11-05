using MiCash.API.Filters;
using MiCash.Application.UseCases.FinancialInsights.Get;
using MiCash.Communication.Requests.FinancialInsights;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.FinancialInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiCash.API.Controllers
{
    [Route("api/financial-insights")]
    [ApiController]
    [Authorize]
    public class FinancialInsightsController : ControllerBase
    {
        [HttpGet]
        [PlanHistoryLimit]
        [ProducesResponseType(
            typeof(ResponseFinancialInsightsJson) ,
            StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status403Forbidden)]
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
