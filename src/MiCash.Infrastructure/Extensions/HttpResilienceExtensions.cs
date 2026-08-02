using MiCash.Infrastructure.Resilience.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace MiCash.Infrastructure.Extensions
{
    public static class HttpResilienceExtensions
    {
        public static IHttpClientBuilder AddMiCashResilience (
            this IHttpClientBuilder builder )
        {
            return builder
                .AddPolicyHandler(HttpRetryPolicy.Create())
                .AddPolicyHandler(HttpTimeoutPolicy.Create())
                .AddPolicyHandler(HttpCircuitBreakerPolicy.Create());
        }
    }
}
