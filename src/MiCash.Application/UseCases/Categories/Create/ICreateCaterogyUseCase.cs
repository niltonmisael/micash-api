using MiCash.Communication.Requests.Categories;

namespace MiCash.Application.UseCases.Categories.Create
{
    public interface ICreateCaterogyUseCase
    {
        Task CreateCaterogy( RequestCreateCategoryJson request , CancellationToken cancellationToken );
    }
}
