using MiCash.Domain.Repositories;

namespace MiCash.Infrastructure.DataAccess
{
    internal class UnitOfWork : IUnitOfWork
    {
        private readonly MiCashDbContext _dbContext;

        public UnitOfWork (MiCashDbContext  dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task Commit (CancellationToken cancellationToken)
        {
           await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
