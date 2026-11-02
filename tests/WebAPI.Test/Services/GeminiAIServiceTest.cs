using FluentAssertions;
using MiCash.Domain.Services.IA;
using MiCash.Exception.ExceptionBase.IA;
using MiCash.Infrastructure.AI.Services;
using MiCash.Infrastructure.Services.IA.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;

namespace WebAPI.Test.Services
{
    public class GeminiAIServiceTest
    {
        private const string PROVIDER_DETAIL = "API key not valid. Project 123456 internal detail";

        [Fact]
        public async Task Unknown_Provider_Error_Is_Not_Exposed_To_Client ()
        {
            var logger = new CapturingLogger();
            var service = CreateService(HttpStatusCode.BadRequest , PROVIDER_DETAIL , logger);

            var act = async () => await service.GenerateResponseAsync(new AIRequest() , CancellationToken.None);

            var exception = (await act.Should().ThrowAsync<AIUnknownException>()).Which;

            exception.Message.Should().NotContain(PROVIDER_DETAIL);
            exception.GetErrors().Should().ContainSingle()
                .Which.Should().Be("Ocorreu um erro inesperado ao comunicar com o serviço de IA.");

            // ...but the detail is kept in the logs for troubleshooting.
            logger.Messages.Should().Contain(m => m.Contains(PROVIDER_DETAIL) && m.Contains("400"));
        }

        [Fact]
        public async Task Invalid_Json_Response_Is_Not_Exposed_To_Client ()
        {
            var service = CreateService(HttpStatusCode.OK , "<html>" + PROVIDER_DETAIL + "</html>" , new CapturingLogger());

            var act = async () => await service.GenerateResponseAsync(new AIRequest() , CancellationToken.None);

            (await act.Should().ThrowAsync<AIUnknownException>())
                .Which.Message.Should().NotContain(PROVIDER_DETAIL);
        }

        [Fact]
        public async Task Client_Cancellation_Is_Not_Reported_As_Provider_Error ()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var service = CreateService(HttpStatusCode.OK , "{}" , new CapturingLogger());

            var act = async () => await service.GenerateResponseAsync(new AIRequest() , cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task Gemini3_Response_Joins_Answer_Parts_And_Skips_Thoughts ()
        {
            const string body = """
            {
              "candidates": [{
                "content": {
                  "role": "model",
                  "parts": [
                    { "text": "Raciocínio interno", "thought": true },
                    { "text": "Você gastou mais com " },
                    { "text": "Mercado.", "thoughtSignature": "abc123" },
                    { "text": "", "thoughtSignature": "def456" }
                  ]
                },
                "finishReason": "STOP"
              }]
            }
            """;

            var service = CreateService(HttpStatusCode.OK , body , new CapturingLogger());

            var response = await service.GenerateResponseAsync(new AIRequest() , CancellationToken.None);

            response.Content.Should().Be("Você gastou mais com Mercado.");
        }

        [Fact]
        public async Task Request_Uses_Gemini_31_Flash_Lite_With_Recommended_Temperature ()
        {
            var handler = new StubHandler(HttpStatusCode.OK , """{"candidates":[{"content":{"parts":[{"text":"ok"}]}}]}""");
            var service = CreateService(handler , new CapturingLogger());

            var request = new AIRequest();
            request.Messages.Add(new AIMessage { Role = MiCash.Domain.Enums.AIMessageRole.User , Content = "Oi" });

            await service.GenerateResponseAsync(request , CancellationToken.None);

            handler.LastRequestUri!.ToString().Should().Contain("models/gemini-3.1-flash-lite:generateContent");

            using var json = System.Text.Json.JsonDocument.Parse(handler.LastRequestBody!);
            json.RootElement.GetProperty("generationConfig").GetProperty("temperature").GetDouble().Should().Be(1.0);

            // Response-only fields must not be sent back to the API.
            handler.LastRequestBody.Should().NotContain("thought");
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized , "{}")]
        [InlineData(HttpStatusCode.Forbidden , "{}")]
        [InlineData(HttpStatusCode.BadRequest , """{"error":{"code":400,"status":"INVALID_ARGUMENT","details":[{"reason":"API_KEY_INVALID"}]}}""")]
        public async Task Invalid_Credential_Is_503_Never_401 ( HttpStatusCode providerStatus , string body )
        {
            var logger = new CapturingLogger();
            var service = CreateService(providerStatus , body , logger);

            var act = async () => await service.GenerateResponseAsync(new AIRequest() , CancellationToken.None);

            // 401 would make the app think the user's session expired and log them out.
            var exception = (await act.Should().ThrowAsync<AIAuthenticationException>()).Which;
            exception.StatusCode.Should().Be(( int ) HttpStatusCode.ServiceUnavailable);
            exception.Message.Should().Be("O serviço de IA encontra-se indisponível.");

            logger.Messages.Should().Contain(m => m.Contains("credencial"));
        }

        [Fact]
        public async Task HttpClient_Timeout_Is_503 ()
        {
            var httpClient = new HttpClient(new SlowHandler())
            {
                BaseAddress = new Uri("https://example.test/") ,
                Timeout = TimeSpan.FromMilliseconds(100)
            };

            var service = new GeminiAIService(
                httpClient ,
                Options.Create(new GeminiSettings { ApiKey = "test" }) ,
                new CapturingLogger());

            var act = async () => await service.GenerateResponseAsync(new AIRequest() , CancellationToken.None);

            (await act.Should().ThrowAsync<AIUnavailableException>())
                .Which.StatusCode.Should().Be(( int ) HttpStatusCode.ServiceUnavailable);
        }

        private sealed class SlowHandler : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync ( HttpRequestMessage request , CancellationToken cancellationToken )
            {
                await Task.Delay(TimeSpan.FromSeconds(10) , cancellationToken);

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        private static GeminiAIService CreateService ( HttpStatusCode status , string body , ILogger<GeminiAIService> logger ) =>
            CreateService(new StubHandler(status , body) , logger);

        private static GeminiAIService CreateService ( StubHandler handler , ILogger<GeminiAIService> logger )
        {
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://example.test/")
            };

            return new GeminiAIService(
                httpClient ,
                Options.Create(new GeminiSettings { ApiKey = "test" }) ,
                logger);
        }

        private sealed class StubHandler ( HttpStatusCode status , string body ) : HttpMessageHandler
        {
            public Uri? LastRequestUri { get; private set; }

            public string? LastRequestBody { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync ( HttpRequestMessage request , CancellationToken cancellationToken )
            {
                cancellationToken.ThrowIfCancellationRequested();

                LastRequestUri = request.RequestUri;
                LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

                return new HttpResponseMessage(status)
                {
                    Content = new StringContent(body , System.Text.Encoding.UTF8 , "application/json")
                };
            }
        }

        private sealed class CapturingLogger : ILogger<GeminiAIService>
        {
            public List<string> Messages { get; } = [];

            public IDisposable? BeginScope<TState> ( TState state ) where TState : notnull => NullLogger.Instance.BeginScope(state);

            public bool IsEnabled ( LogLevel logLevel ) => true;

            public void Log<TState> ( LogLevel logLevel , EventId eventId , TState state , System.Exception? exception , Func<TState , System.Exception? , string> formatter )
            {
                Messages.Add(formatter(state , exception));
            }
        }
    }
}
