using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Plans;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    internal class PlanRepository : IPlansReadOnlyRepository
    {
        private readonly MiCashDbContext _dbContext;

        public PlanRepository ( MiCashDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<IEnumerable<Plan>> GetAllActiveAsync (CancellationToken cancellationToken )
        {
            return await _dbContext.Plans.Where(p => p.IsActive).ToListAsync(cancellationToken);
        }

        public async Task<Plan?> GetByIdAsync ( long planId, CancellationToken cancellationToken )
        {
            return await _dbContext.Plans.FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
        }
    }
}
