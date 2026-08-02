using Polly;
using Polly.Extensions.Http;

namespace MiCash.Infrastructure.Resilience.Policies
{
    public static class HttpRetryPolicy
    {
        public static IAsyncPolicy<HttpResponseMessage> Create ()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()

                .OrResult(response =>
                    response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)

                .WaitAndRetryAsync(
                    retryCount: 3 ,

                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2 , retryAttempt))
                );
        }
    }
}
