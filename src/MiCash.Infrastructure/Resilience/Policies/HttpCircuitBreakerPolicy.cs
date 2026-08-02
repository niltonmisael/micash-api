using Polly;
using Polly.Extensions.Http;

namespace MiCash.Infrastructure.Resilience.Policies
{
    public static class HttpCircuitBreakerPolicy
    {
        public static IAsyncPolicy<HttpResponseMessage> Create ()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()

                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking:
                        HttpPolicyConstants.CircuitBreakerFailures ,

                    durationOfBreak:
                        HttpPolicyConstants.CircuitBreakDuration
                );
        }
    }
}
