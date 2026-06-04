using MiCash.Communication.Responses.Categories;

namespace MiCash.Application.UseCases.Categories.GetAll
{
    public interface IGetAllCategoryUseCase
    {
        Task<ResponseCategoriesJson> Execute ( CancellationToken cancellationToken );
    }
}
