using MiCash.Communication.Responses.Categories;

namespace MiCash.Application.UseCases.Categories.GetById
{
    public interface IGetCategoryByIdUseCase
    {
        Task<IEnumerable<ResponseCategoryJson>> Execute ( long id, CancellationToken cancellationToken );
    }
}
