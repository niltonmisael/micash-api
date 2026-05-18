using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Categories
{
    public interface ICategoriesWriteOnlyRepository
    {
        Task Create ( Category category );
    }
}
