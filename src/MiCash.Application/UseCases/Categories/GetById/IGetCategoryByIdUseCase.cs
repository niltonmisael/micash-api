using MiCash.Communication.Responses.Categories;

namespace MiCash.Application.UseCases.Categories.GetById
{
    public interface IGetCategoryByIdUseCase
    {
        Task<IEnumerable<ResponseCategoriesJson>> Execute ( long id, CancellationToken cancellationToken );
    }
}
