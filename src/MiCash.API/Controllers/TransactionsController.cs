using MiCash.Application.UseCases.Transactions.GetAll;
using MiCash.Communication.Pagination;
using MiCash.Communication.Requests.Transactions;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiCash.API.Controllers
{
    [Route("api/transactions")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        /// <summary>
        /// Incomes and expenses of one month (Brasília calendar), newest first, with optional type, category and
        /// title filters. Always 200 (an empty page has data = []). Not limited by plan.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ResponsePagedJson<ResponseTransactionJson>) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll (
            [FromServices] IGetTransactionsUseCase useCase ,
            [FromQuery] RequestTransactionsJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(request , cancellationToken);

            return Ok(response);
        }
    }
}
