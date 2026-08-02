using Polly;
using Polly.Timeout;

namespace MiCash.Infrastructure.Resilience.Policies
{
    public static class HttpTimeoutPolicy
    {
        public static IAsyncPolicy<HttpResponseMessage> Create ()
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(
                timeout: TimeSpan.FromSeconds(
                    HttpPolicyConstants.TimeoutInSeconds) ,

                timeoutStrategy: TimeoutStrategy.Optimistic);
        }
    }
}
