namespace MiCash.Infrastructure.Resilience.Policies
{
    public static class HttpPolicyConstants
    {
        public const int RetryCount = 3;

        public const int TimeoutInSeconds = 30;

        public const int CircuitBreakerFailures = 5;

        public static readonly TimeSpan RetryDelay =
            TimeSpan.FromSeconds(2);

        public static readonly TimeSpan CircuitBreakDuration =
            TimeSpan.FromSeconds(30);
    }
}
