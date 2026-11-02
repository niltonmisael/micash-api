using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace MiCash.Infrastructure.Resilience.Policies
{
    /// <summary>
    /// Resilience for the AI provider. Differs from the payment policies on purpose:
    /// - 429 is NOT retried: retrying a rate limit only burns more quota and makes the user wait;
    /// - a single quick retry for transient failures (5xx, 408, network, per-try timeout);
    /// - per-try timeout sized so that try + delay + retry fits inside HttpClient.Timeout.
    /// </summary>
    public static class AIHttpPolicies
    {
        public const int RetryCount = 1;

        public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

        public static readonly TimeSpan PerTryTimeout = TimeSpan.FromSeconds(12);

        // Must be greater than (RetryCount + 1) * PerTryTimeout + RetryCount * RetryDelay = 25s.
        public static readonly TimeSpan TotalTimeout = TimeSpan.FromSeconds(30);

        public static IAsyncPolicy<HttpResponseMessage> Retry ()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(
                    retryCount: RetryCount ,
                    sleepDurationProvider: _ => RetryDelay);
        }

        public static IAsyncPolicy<HttpResponseMessage> Timeout ()
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(
                PerTryTimeout ,
                TimeoutStrategy.Optimistic);
        }
    }
}
