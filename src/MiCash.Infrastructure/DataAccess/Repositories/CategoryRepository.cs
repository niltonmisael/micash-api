using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Categories;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    public class CategoryRepository : ICategoriesWriteOnlyRepository, ICategoriesReadOnlyRepository, ICategoriesUpdateOnlyRepository
    {
        private readonly MiCashDbContext _dbContext;

        public CategoryRepository ( MiCashDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task Create ( Category category )
        {
            await _dbContext.Categories.AddAsync(category);

        }

        public async Task<IEnumerable<Category>> GetAllAsync ( User user , CancellationToken cancellationToken )
        {
            return await _dbContext.Categories
                .AsNoTracking()
                .Where(f => f.UserId == user.Id)
                .Where(f => f.IsActive == true)
                .ToListAsync(cancellationToken);
        }

        public Task<Category?> GetByIdAsync ( User loggedUser , long id , CancellationToken cancellationToken )
        {
            return _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == loggedUser.Id , cancellationToken);
        }

        public async Task InactivateAsync ( long categoryId , CancellationToken cancellationToken )
        {
            await _dbContext.Categories
                .Where(f => f.Id == categoryId)
                .ExecuteUpdateAsync(f => f
                    .SetProperty(p => p.IsActive , false)
                    .SetProperty(p => p.UpdatedAt , DateTime.UtcNow) , cancellationToken);
        }

        public async Task UpdateAsync (
            long categoryId ,
            string name ,
            string color ,
            string icon ,
            CancellationToken cancellationToken )
        {
            await _dbContext.Categories
                .Where(c => c.Id == categoryId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.Name , name)
                    .SetProperty(c => c.Color , color)
                    .SetProperty(c => c.Icon , icon)
                    .SetProperty(c => c.UpdatedAt , DateTime.UtcNow) ,
                    cancellationToken);
        }
    }
}
