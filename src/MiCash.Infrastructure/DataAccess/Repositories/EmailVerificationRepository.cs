using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.EmailVerifications;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    public sealed class EmailVerificationRepository :
        IEmailVerificationReadOnlyRepository,
        IEmailVerificationWriteOnlyRepository,
        IEmailVerificationUpdateOnlyRepository
    {
        private readonly MiCashDbContext _dbContext;

        public EmailVerificationRepository ( MiCashDbContext dbContext )
        {
            _dbContext = dbContext;
        }

        public async Task<int> CountCreatedSinceAsync ( long userId , DateTime since , CancellationToken cancellationToken )
        {
            return await _dbContext.EmailVerificationRequests
                .AsNoTracking()
                .CountAsync(r => r.UserId == userId && r.CreatedAt >= since , cancellationToken);
        }

        public async Task<DateTime?> GetLatestCreatedAtAsync ( long userId , EmailVerificationPurpose purpose , CancellationToken cancellationToken )
        {
            return await _dbContext.EmailVerificationRequests
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.Purpose == purpose)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => (DateTime?)r.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task AddAsync ( EmailVerificationRequest request , CancellationToken cancellationToken )
        {
            await _dbContext.EmailVerificationRequests.AddAsync(request , cancellationToken);
        }

        public async Task<EmailVerificationRequest?> GetByIdAsync ( long id , CancellationToken cancellationToken )
        {
            return await _dbContext.EmailVerificationRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id , cancellationToken);
        }

        public async Task<List<EmailVerificationRequest>> GetOpenByUserAsync ( long userId , CancellationToken cancellationToken )
        {
            return await _dbContext.EmailVerificationRequests
                .Where(r => r.UserId == userId && r.VerifiedAt == null && r.InvalidatedAt == null)
                .ToListAsync(cancellationToken);
        }

        public async Task<EmailVerificationRequest?> GetPendingVerificationAsync ( long userId , EmailVerificationPurpose purpose , CancellationToken cancellationToken )
        {
            return await _dbContext.EmailVerificationRequests
                .Where(r => r.UserId == userId
                    && r.Purpose == purpose
                    && r.VerifiedAt == null
                    && r.InvalidatedAt == null
                    && r.CodeHash != null)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
