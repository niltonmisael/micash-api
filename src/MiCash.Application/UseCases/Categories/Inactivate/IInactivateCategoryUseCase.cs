namespace MiCash.Application.UseCases.Categories.Inactivate
{
    public interface IInactivateCategoryUseCase
    {
        Task Execute ( long id , CancellationToken cancellationToken );
    }
}
