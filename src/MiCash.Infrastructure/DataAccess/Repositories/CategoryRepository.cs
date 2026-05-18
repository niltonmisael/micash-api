using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Categories;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    public class CategoryRepository : ICategoriesWriteOnlyRepository
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
    }
}
