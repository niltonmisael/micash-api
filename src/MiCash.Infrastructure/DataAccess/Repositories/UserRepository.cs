using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Users;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    internal class UserRepository : IUserReadOnlyRepository, IUserWriteOnlyRepository
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

        public async Task<bool> ExistActiveUserWithEmail ( string email )
        {
           return await _dbContext.Users.AnyAsync(user =>  user.Email.Equals(email));
        }

        public async Task<User?> GetUserByEmail ( string email )
        {
            return await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Email == email);
        }
    }
}
