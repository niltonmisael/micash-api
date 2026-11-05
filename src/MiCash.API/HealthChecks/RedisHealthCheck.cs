using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace MiCash.API.HealthChecks
{
    /// <summary>
    /// Redis: the cache service has no fallback (a Redis error fails the request), so an unreachable Redis is Unhealthy.
    /// </summary>
    public sealed class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisHealthCheck ( IConnectionMultiplexer redis )
        {
            _redis = redis;
        }

        public async Task<HealthCheckResult> CheckHealthAsync (
            HealthCheckContext context ,
            CancellationToken cancellationToken = default )
        {
            try {
                await _redis.GetDatabase().PingAsync();
                return HealthCheckResult.Healthy();
            } catch ( System.Exception ex ) {
                return HealthCheckResult.Unhealthy("Redis unreachable." , ex);
            }
        }
    }
}
