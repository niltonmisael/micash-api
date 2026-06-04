using MiCash.Communication.Responses.Categories;

namespace MiCash.Application.UseCases.Categories.GetAll
{
    public interface IGetAllCategoryUseCase
    {
        Task<ResponseCategoryJson> Execute ( CancellationToken cancellationToken );
    }
}
