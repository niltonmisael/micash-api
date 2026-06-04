using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Categories
{
    public interface ICategoriesUpdateOnlyRepository
    {
        Task InactivateAsync ( long categoryId , CancellationToken cancellationToken );
        Task UpdateAsync (
            long categoryId ,
            string name ,
            string color ,
            string icon,
            CancellationToken cancellationToken );
    }
}
