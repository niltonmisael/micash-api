using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Categories
{
    public interface ICategoriesReadOnlyRepository
    {
        Task<IEnumerable<Category>> GetAllAsync ( User user, CancellationToken cancellationToken );
        Task<Category?> GetByIdAsync ( User loggedUser , long id , CancellationToken cancellationToken );
    }
}
