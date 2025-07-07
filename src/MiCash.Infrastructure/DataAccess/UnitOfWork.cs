using MiCash.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Infrastructure.DataAccess
{
    internal class UnitOfWork : IUnitOfWork
    {
        private readonly MiCashDbContext _dbContext;

        public UnitOfWork (MiCashDbContext  dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task Commit ()
        {
           await _dbContext.SaveChangesAsync();
        }
    }
}
