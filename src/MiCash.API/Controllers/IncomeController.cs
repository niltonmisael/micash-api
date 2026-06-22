using MiCash.Application.UseCases.Incomes.Delete;
using MiCash.Application.UseCases.Incomes.GetAll;
using MiCash.Application.UseCases.Incomes.GetById;
using MiCash.Application.UseCases.Incomes.Register;
using MiCash.Application.UseCases.Incomes.Update;
using MiCash.Communication.Pagination;
using MiCash.Communication.Requests.Incomes;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.Incomes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiCash.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class IncomeController : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(typeof(ResponseRegisteredIncomeJson) , StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterIncome (
            [FromServices] IRegisterIncomeUseCase useCase ,
            [FromBody] RequestIncomeJson request ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(request , cancellationToken);

            return Created(string.Empty , response);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResponseIncomesJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetAllIncomes ( 
            [FromServices] IGetAllIncomesUseCase useCase,
            [FromQuery] RequestPaginationJson request ,
            CancellationToken cancellationToken)
        {
            var response = await useCase.Execute(request,cancellationToken);

            if ( response.Data.Count != 0 ) {
                return Ok(response);
            }
            return NoContent();
        }

        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(typeof(ResponseIncomeJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetIncomeById (
            [FromServices] IGetIncomeByIdUseCase useCase ,
            [FromRoute] long id,
            CancellationToken cancellationToken)
        {
            var response = await useCase.Execute(id, cancellationToken);

            return Ok(response);
        }

        [HttpPut]
        [Route("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateIncome (
            [FromServices] IUpdateIncomeUseCase useCase ,
            [FromRoute] long id ,
            [FromBody] RequestIncomeJson request ,
            CancellationToken cancellationToken )
        {
            await useCase.Execute(id , request , cancellationToken);

            return NoContent();
        }

        [HttpDelete]
        [Route("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete (
            [FromServices] IDeleteIncomeUseCase useCase ,
            [FromRoute] long id ,
            CancellationToken cancellationToken )
        {
            await useCase.Execute(id , cancellationToken);

            return NoContent();
        }
    }
}
