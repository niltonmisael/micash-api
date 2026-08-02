using MiCash.Domain.Enums;
using MiCash.Domain.Services.IA;
using MiCash.Exception.ExceptionBase.IA;
using MiCash.Infrastructure.Services.IA.Configuration;
using MiCash.Infrastructure.Services.IA.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net;
using System.Net.Http.Json;

namespace MiCash.Infrastructure.AI.Services;

public sealed class GeminiAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiAIService> _logger;

    public GeminiAIService (
        HttpClient httpClient ,
        IOptions<GeminiSettings> settings ,
        ILogger<GeminiAIService> logger )
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AIResponse> GenerateResponseAsync (
        AIRequest request ,
        CancellationToken cancellationToken )
    {
        try {
            var geminiRequest = CreateGeminiRequest(request);

            var response = await _httpClient.PostAsJsonAsync(
                _settings.GenerateContentEndpoint ,
                geminiRequest ,
                cancellationToken);

            await EnsureSuccessStatusCode(
                response ,
                cancellationToken);

            var geminiResponse =
                await response.Content.ReadFromJsonAsync<GeminiResponse>(
                    cancellationToken: cancellationToken);

            ValidateResponse(geminiResponse);

            return BuildResponse(geminiResponse!);
        } catch ( BrokenCircuitException ex ) {
            _logger.LogError(
                ex ,
                "Circuit Breaker aberto para comunicação com o Gemini.");

            throw new AIUnavailableException();
        } catch ( TimeoutRejectedException ex ) {
            _logger.LogError(
                ex ,
                "Timeout ao comunicar com o Gemini.");

            throw new AIUnavailableException();
        } catch ( HttpRequestException ex ) {
            _logger.LogError(
                ex ,
                "Erro HTTP ao comunicar com o Gemini.");

            throw new AIUnavailableException();
        } catch ( AIServiceException ) {
            throw;
        } catch ( System.Exception ex ) {
            _logger.LogError(
                ex ,
                "Erro inesperado ao comunicar com o Gemini.");

            throw new AIUnknownException(
                "Ocorreu um erro inesperado ao comunicar com o serviço de IA.");
        }
    }

    private static GeminiRequest CreateGeminiRequest (
        AIRequest request )
    {
        return new GeminiRequest
        {
            Contents = request.Messages
                .Select(message => new GeminiContent
                {
                    Role = message.Role switch
                    {
                        AIMessageRole.User => "user",

                        AIMessageRole.Assistant => "model",

                        AIMessageRole.System => "user",

                        _ => "user"
                    } ,

                    Parts =
                    [
                        new GeminiPart
                        {
                            Text = message.Content
                        }
                    ]
                })
                .ToList() ,

            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = request.Options.Temperature ,

                TopP = request.Options.TopP ,

                MaxOutputTokens = request.Options.MaxOutputTokens
            }
        };
    }

    private static async Task EnsureSuccessStatusCode (
        HttpResponseMessage response ,
        CancellationToken cancellationToken )
    {
        if ( response.IsSuccessStatusCode )
            return;

        switch ( response.StatusCode ) {
            case HttpStatusCode.Unauthorized:
            case HttpStatusCode.Forbidden:
                throw new AIAuthenticationException();

            case HttpStatusCode.TooManyRequests:
                throw new AIRateLimitException();

            case HttpStatusCode.InternalServerError:
            case HttpStatusCode.BadGateway:
            case HttpStatusCode.ServiceUnavailable:
            case HttpStatusCode.GatewayTimeout:
                throw new AIUnavailableException();

            default:

                var error =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                throw new AIUnknownException(
                    $"Erro ao comunicar com o Gemini: {error}");
        }
    }

    private static void ValidateResponse (
        GeminiResponse? response )
    {
        if ( response is null )
            throw new AIInvalidResponseException();

        if ( response.Candidates is null ||
            response.Candidates.Count == 0 ) {
            throw new AIInvalidResponseException();
        }

        var candidate = response.Candidates[0];

        if ( candidate.Content is null )
            throw new AIInvalidResponseException();

        if ( candidate.Content.Parts is null ||
            candidate.Content.Parts.Count == 0 ) {
            throw new AIInvalidResponseException();
        }

        var part = candidate.Content.Parts[0];

        if ( string.IsNullOrWhiteSpace(part.Text) )
            throw new AIInvalidResponseException();
    }

    private static AIResponse BuildResponse (
        GeminiResponse response )
    {
        var candidate = response.Candidates[0];

        return new AIResponse
        {
            Content = candidate.Content.Parts[0].Text
        };
    }
}