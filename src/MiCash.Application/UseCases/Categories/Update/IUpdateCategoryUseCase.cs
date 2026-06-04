using MiCash.Communication.Requests.Categories;

namespace MiCash.Application.UseCases.Categories.Update
{
    public interface IUpdateCategoryUseCase
    {
        Task Execute (long categoryId, RequestUpdateCategoryJson request , CancellationToken cancellationToken );
    }
}
