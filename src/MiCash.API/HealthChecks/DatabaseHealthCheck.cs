using MiCash.Infrastructure.DataAccess;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MiCash.API.HealthChecks
{
    /// <summary>PostgreSQL: the API answers nothing useful without the database.</summary>
    public sealed class DatabaseHealthCheck : IHealthCheck
    {
        private readonly MiCashDbContext _dbContext;

        public DatabaseHealthCheck ( MiCashDbContext dbContext )
        {
            _dbContext = dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync (
            HealthCheckContext context ,
            CancellationToken cancellationToken = default )
        {
            try {
                return await _dbContext.Database.CanConnectAsync(cancellationToken)
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy("Database unreachable.");
            } catch ( System.Exception ex ) {
                return HealthCheckResult.Unhealthy("Database unreachable." , ex);
            }
        }
    }
}
