using MiCash.Domain.Repositories;
using MiCash.Exception.ExceptionBase;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MiCash.Infrastructure.DataAccess
{
    internal class UnitOfWork : IUnitOfWork
    {
        private readonly MiCashDbContext _dbContext;

        public UnitOfWork ( MiCashDbContext dbContext )
        {
            _dbContext = dbContext;
        }

        public async Task Commit ( CancellationToken cancellationToken )
        {
            try {
                await _dbContext.SaveChangesAsync(cancellationToken);
            } catch ( DbUpdateConcurrencyException ) {
                // A concurrency token (e.g. savings goal xmin) changed since it was read.
                // SaveChanges runs in a transaction, so nothing from this commit was persisted.
                _dbContext.ChangeTracker.Clear();

                throw new ConflictException();
            } catch ( DbUpdateException ex ) when ( ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } ) {
                // The failed entities stay tracked as Added/Modified; clear them so the request
                // can re-read the winning row (or fail cleanly) instead of retrying the same insert.
                _dbContext.ChangeTracker.Clear();

                throw new ConflictException();
            }
        }
    }
}
