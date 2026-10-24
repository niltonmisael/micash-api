using MiCash.Application.UseCases.Categories.Create;
using MiCash.Application.UseCases.Categories.GetAll;
using MiCash.Application.UseCases.Categories.GetById;
using MiCash.Application.UseCases.Categories.Inactivate;
using MiCash.Application.UseCases.Categories.Update;
using MiCash.Application.UseCases.Expenses.Delete;
using MiCash.Communication.Requests.Categories;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiCash.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoryController : Controller
    {
        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCategory ( 
            [FromBody] RequestCreateCategoryJson request, 
            [FromServices] ICreateCaterogyUseCase useCase,
            CancellationToken cancellationToken)
        {
            await useCase.CreateCaterogy(request, cancellationToken);

            return Created();
        }

        [HttpGet("all")]
        [ProducesResponseType(typeof(ResponseCategoriesJson) , StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllCategory (
            [FromServices] IGetAllCategoryUseCase useCase ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(cancellationToken);

            if ( response.Categories.Any() ) 
            {
                return Ok(response);
            } 
            else 
            {
                return NoContent();
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ResponseCategoryJson) , StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCategoryById (
            [FromRoute] long id ,
            [FromServices] IGetCategoryByIdUseCase useCase ,
            CancellationToken cancellationToken )
        {
            var response = await useCase.Execute(id , cancellationToken);

            return Ok(response);
        }

        [HttpDelete]
        [Route("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Inactivate ( 
            [FromServices] IInactivateCategoryUseCase useCase , 
            [FromRoute] long id ,
            CancellationToken cancellationToken)
        {
            await useCase.Execute(id, cancellationToken);

            return NoContent();
        }

        [HttpPut]
        [Route("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErrorJson) , StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCategory (
            [FromServices] IUpdateCategoryUseCase useCase ,
            [FromBody] RequestUpdateCategoryJson request ,
            [FromRoute] long id ,
            CancellationToken cancellationToken )
        {
            await useCase.Execute(id , request, cancellationToken);

            return NoContent();
        }
    }
}
