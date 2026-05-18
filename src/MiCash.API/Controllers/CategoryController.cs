using MiCash.Application.UseCases.Categories.Create;
using MiCash.Communication.Requests.Categories;
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
        //[ProducesResponseType()]
        public async Task<IActionResult> CreateCategory ( 
            [FromBody] RequestCreateCategoryJson request, 
            [FromServices] ICreateCaterogyUseCase useCase,
            CancellationToken cancellationToken)
        {
            await useCase.CreateCaterogy(request, cancellationToken);

            return Created();
        }
    }
}
