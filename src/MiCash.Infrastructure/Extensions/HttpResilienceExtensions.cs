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

        /// <summary>
        /// Policies for the AI provider (see <see cref="AIHttpPolicies"/>).
        /// Order: retry (outer) → per-try timeout → circuit breaker (inner).
        /// </summary>
        public static IHttpClientBuilder AddAIResilience (
            this IHttpClientBuilder builder )
        {
            return builder
                .AddPolicyHandler(AIHttpPolicies.Retry())
                .AddPolicyHandler(AIHttpPolicies.Timeout())
                .AddPolicyHandler(HttpCircuitBreakerPolicy.Create());
        }
    }
}
