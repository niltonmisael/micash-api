using MiCash.Application.UseCases.Dashboards.DailySummary;
using MiCash.Application.UseCases.Dashboards.ExpensesByCategory;
using MiCash.Application.UseCases.Dashboards.LatestExpenses;
using MiCash.Application.UseCases.Dashboards.MonthlySummary;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.Dashboards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiCash.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {

        [HttpGet("monthly-summary")]
        [ProducesResponseType(typeof(ResponseMonthlySummaryJson) , StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMonthlySummary (
            [FromServices] IGetMonthlySummaryUseCase useCase ,
            [FromQuery] int year ,
            [FromQuery] int month ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(
                year ,
                month ,
                cancellationToken);

            return Ok(response);
        }

        /// <summary>
        /// Income and expenses per day of the month (every day, 0 when there is nothing), with running totals.
        /// The last day's cumulative values equal the monthly-summary totals of the same month.
        /// </summary>
        [HttpGet("daily-summary")]
        [ProducesResponseType(typeof(ResponseDailySummaryJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDailySummary (
            [FromServices] IGetDailySummaryUseCase useCase ,
            [FromQuery] int year ,
            [FromQuery] int month ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(year , month , cancellationToken);

            return Ok(response);
        }

        [HttpGet("expenses-by-category")]
        [ProducesResponseType(typeof(List<ResponseExpenseCategorySummaryJson>) , StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExpensesByCategory (
            [FromServices] IGetExpensesByCategoryUseCase useCase ,
            [FromQuery] int year ,
            [FromQuery] int month ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(
                year ,
                month ,
                cancellationToken);

            return Ok(response);
        }

        [HttpGet("lastest-expenses")]
        [ProducesResponseType(typeof(List<ResponseLatestExpenseJson>) , StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetLatestExpenses (
            [FromServices] IGetLatestExpensesUseCase useCase ,
            [FromQuery] int count ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(
                count ,
                cancellationToken);

            if(response.Count != 0 ) {

                return Ok(response);
            }

            return NoContent();
        }
    }
}
