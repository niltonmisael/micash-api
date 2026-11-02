using FluentAssertions;
using MiCash.Infrastructure.Extensions;
using MiCash.Infrastructure.Resilience.Policies;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace WebAPI.Test.Services
{
    public class AIResiliencePolicyTest
    {
        [Fact]
        public async Task Rate_Limit_Is_Not_Retried ()
        {
            var handler = new CountingHandler(HttpStatusCode.TooManyRequests);

            var response = await CreateClient(handler).GetAsync("https://example.test/");

            response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            handler.Calls.Should().Be(1);
        }

        [Fact]
        public async Task Transient_Error_Is_Retried_Once ()
        {
            var handler = new CountingHandler(HttpStatusCode.ServiceUnavailable);

            var response = await CreateClient(handler).GetAsync("https://example.test/");

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            handler.Calls.Should().Be(1 + AIHttpPolicies.RetryCount);
        }

        [Fact]
        public void Worst_Case_Fits_Inside_HttpClient_Timeout ()
        {
            var worstCase =
                AIHttpPolicies.PerTryTimeout * ( AIHttpPolicies.RetryCount + 1 ) +
                AIHttpPolicies.RetryDelay * AIHttpPolicies.RetryCount;

            worstCase.Should().BeLessThan(AIHttpPolicies.TotalTimeout);
        }

        private static HttpClient CreateClient ( CountingHandler handler )
        {
            var services = new ServiceCollection();

            services
                .AddHttpClient("ai")
                .ConfigurePrimaryHttpMessageHandler(() => handler)
                .AddAIResilience();

            return services
                .BuildServiceProvider()
                .GetRequiredService<IHttpClientFactory>()
                .CreateClient("ai");
        }

        private sealed class CountingHandler ( HttpStatusCode status ) : HttpMessageHandler
        {
            public int Calls { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync ( HttpRequestMessage request , CancellationToken cancellationToken )
            {
                Calls++;

                return Task.FromResult(new HttpResponseMessage(status));
            }
        }
    }
}
