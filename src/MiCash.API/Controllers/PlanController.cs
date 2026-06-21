using MiCash.Application.UseCases.Plans.GetAll;
using MiCash.Application.UseCases.Plans.GetById;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.Plans;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MiCash.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlanController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ResponsePlansJson>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson),StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAllPlans (
            [FromServices] IGetAllPlansUseCase useCase ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.ExecuteAsync(cancellationToken);

            return Ok(response);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ResponsePlansJson) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPlanById (
            [FromServices] IGetPlanByIdUseCase useCase ,
            long id ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.ExecuteAsync(id, cancellationToken);

            return Ok(response);
        }
    }
}
