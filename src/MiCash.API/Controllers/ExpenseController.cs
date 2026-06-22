using MiCash.Application.UseCases.Expenses.Delete;
using MiCash.Application.UseCases.Expenses.GetAll;
using MiCash.Application.UseCases.Expenses.GetById;
using MiCash.Application.UseCases.Expenses.Register;
using MiCash.Application.UseCases.Expenses.Update;
using MiCash.Communication.Pagination;
using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiCash.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExpenseController : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(typeof(ResponseRegisteredExpenseJson),StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErrorJson),StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register ( 
            [FromServices] IRegisterExpenseUseCase useCase ,
            [FromBody] RequestExpenseJson request,
            CancellationToken cancellationToken)
        {
                var response = await useCase.Execute(request, cancellationToken);

                return Created(string.Empty, response);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResponseExpensesJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetAllExpenses ( 
            [FromServices] IGetAllExpensesUseCase useCase,
            [FromQuery] RequestPaginationJson request,
            CancellationToken cancellationToken)
        {
            var response = await useCase.Execute(request,cancellationToken);

            if (response.Data.Count != 0 ) {
                return Ok(response);
            }
            return NoContent();
        }
        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(typeof(ResponseExpenseJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById ( 
            [FromServices] IGetExpenseByIdUseCase useCase, 
            [FromRoute] long id,
            CancellationToken cancellationToken)
        {
            var response = await useCase.Execute(id, cancellationToken);

            return Ok(response);
        }

        [HttpDelete]
        [Route("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete ( 
            [FromServices] IDeleteExpenseUseCase useCase, 
            [FromRoute] long id,
            CancellationToken cancellationToken)
        {
            await useCase.Execute(id, cancellationToken);

            return NoContent();
        }

        [HttpPut]
        [Route("{id}")]
        [ProducesResponseType(typeof(ResponseExpensesJson) , StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update ( 
            [FromServices] IUpdateExpenseUseCase useCase , 
            [FromRoute] long id, 
            [FromBody] RequestExpenseJson request,
            CancellationToken cancellationToken)
        {
            await useCase.Execute(id, request, cancellationToken);

            return NoContent();
        }
    }
}
