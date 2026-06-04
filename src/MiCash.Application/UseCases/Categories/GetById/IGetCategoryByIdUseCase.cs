using MiCash.Communication.Responses.Categories;

namespace MiCash.Application.UseCases.Categories.GetById
{
    public interface IGetCategoryByIdUseCase
    {
        Task<ResponseCategoryJson> Execute ( long id, CancellationToken cancellationToken );
    }
}
