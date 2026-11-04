using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Users;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    internal class UserRepository : IUserReadOnlyRepository, IUserWriteOnlyRepository, IUserUpdateOnlyRepository
    {
        private readonly MiCashDbContext _dbContext;

        public UserRepository ( MiCashDbContext dbContext )
        {
            _dbContext = dbContext;
        }

        public async Task Add ( User user )
        {
            await _dbContext.Users.AddAsync ( user );
        }

        public async Task Delete ( User user )
        {
            var userToRemove = await _dbContext.Users.FindAsync ( user.Id );
            _dbContext.Users.Remove(userToRemove!);
        }

        public async Task<int> RemoveUnverifiedCreatedBeforeAsync ( DateTime createdBefore , int batchSize , CancellationToken cancellationToken )
        {
            // Same path as the account deletion (the database cascades the dependent rows).
            var users = await _dbContext.Users
                .Where(user => user.EmailVerifiedAt == null && user.CreatedAt < createdBefore)
                .OrderBy(user => user.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            _dbContext.Users.RemoveRange(users);

            return users.Count;
        }

        public async Task<bool> ExistActiveUserWithEmail ( string email )
        {
           return await _dbContext.Users.AnyAsync(user =>  user.Email.Equals(email));
        }

        public async Task<User> GetById ( long id )
        {
            return await _dbContext.Users.FirstAsync(user => user.Id == id);
        }

        public async Task<IReadOnlyList<long>> GetMonthlySummaryRecipientIdsAsync ( DateOnly month , long afterId , int take , CancellationToken cancellationToken )
        {
            return await _dbContext.Users
                .AsNoTracking()
                .Where(user =>
                    user.Id > afterId &&
                    user.EmailVerifiedAt != null &&
                    ( user.MonthlySummaryEmail || user.MonthlySummaryPush ) &&
                    ( user.MonthlySummarySentFor == null || user.MonthlySummarySentFor < month ))
                .OrderBy(user => user.Id)
                .Select(user => user.Id)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<User?> GetByIdAsync ( long userId , CancellationToken cancellationToken )
        {
            return await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
        }

        public async Task<User?> GetByUserIdentifier ( Guid userIdentifier )
        {
            return await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(user => user.UserIdentifier == userIdentifier);
        }

        public async Task<User?> GetUserByEmail ( string email )
        {
            return await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Email == email);
        }

        public void Update ( User user )
        {
            _dbContext.Users.Update( user );
        }
    }
}
