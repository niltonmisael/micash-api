using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.PasswordResets;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    public sealed class PasswordResetRepository :
        IPasswordResetReadOnlyRepository,
        IPasswordResetWriteOnlyRepository,
        IPasswordResetUpdateOnlyRepository
    {
        private readonly MiCashDbContext _dbContext;

        public PasswordResetRepository ( MiCashDbContext dbContext )
        {
            _dbContext = dbContext;
        }

        public async Task<int> CountCreatedSinceAsync ( long userId , DateTime since , CancellationToken cancellationToken )
        {
            return await _dbContext.PasswordResetRequests
                .AsNoTracking()
                .CountAsync(r => r.UserId == userId && r.CreatedAt >= since , cancellationToken);
        }

        public async Task<DateTime?> GetLatestCreatedAtAsync ( long userId , CancellationToken cancellationToken )
        {
            return await _dbContext.PasswordResetRequests
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => (DateTime?)r.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task AddAsync ( PasswordResetRequest request , CancellationToken cancellationToken )
        {
            await _dbContext.PasswordResetRequests.AddAsync(request , cancellationToken);
        }

        public async Task<PasswordResetRequest?> GetByIdAsync ( long id , CancellationToken cancellationToken )
        {
            return await _dbContext.PasswordResetRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id , cancellationToken);
        }

        public async Task<List<PasswordResetRequest>> GetOpenByUserAsync ( long userId , CancellationToken cancellationToken )
        {
            return await _dbContext.PasswordResetRequests
                .Where(r => r.UserId == userId && r.CompletedAt == null && r.InvalidatedAt == null)
                .ToListAsync(cancellationToken);
        }

        public async Task<PasswordResetRequest?> GetPendingVerificationAsync ( long userId , CancellationToken cancellationToken )
        {
            return await _dbContext.PasswordResetRequests
                .Where(r => r.UserId == userId
                    && r.CompletedAt == null
                    && r.InvalidatedAt == null
                    && r.VerifiedAt == null
                    && r.CodeHash != null)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<PasswordResetRequest?> GetByResetTokenHashAsync ( string resetTokenHash , CancellationToken cancellationToken )
        {
            return await _dbContext.PasswordResetRequests
                .FirstOrDefaultAsync(r => r.ResetTokenHash == resetTokenHash , cancellationToken);
        }
    }
}
