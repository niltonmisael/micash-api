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
        } catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
            // The client cancelled the request: not a provider failure.
            throw;
        } catch ( OperationCanceledException ex ) {
            // HttpClient.Timeout elapsed (TaskCanceledException without client cancellation).
            _logger.LogError(
                ex ,
                "Timeout total ao comunicar com o Gemini.");

            throw new AIUnavailableException();
        } catch ( System.Exception ex ) {
            _logger.LogError(
                ex ,
                "Erro inesperado ao comunicar com o Gemini.");

            throw new AIUnknownException();
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

    private async Task EnsureSuccessStatusCode (
        HttpResponseMessage response ,
        CancellationToken cancellationToken )
    {
        if ( response.IsSuccessStatusCode )
            return;

        switch ( response.StatusCode ) {
            case HttpStatusCode.Unauthorized:
            case HttpStatusCode.Forbidden:
                _logger.LogCritical(
                    "Gemini recusou a credencial ({StatusCode}). Verifique Gemini:ApiKey e o projeto no Google AI Studio." ,
                    ( int ) response.StatusCode);

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

                // Gemini answers an invalid key with 400 INVALID_ARGUMENT + reason API_KEY_INVALID.
                if ( response.StatusCode == HttpStatusCode.BadRequest &&
                    error.Contains("API_KEY_INVALID" , StringComparison.Ordinal) ) {
                    _logger.LogCritical(
                        "Gemini recusou a credencial (API_KEY_INVALID). Verifique Gemini:ApiKey.");

                    throw new AIAuthenticationException();
                }

                // Provider details stay in the logs only; the client gets a generic message.
                _logger.LogError(
                    "Gemini retornou {StatusCode}: {ErrorBody}" ,
                    ( int ) response.StatusCode ,
                    Truncate(error , MAX_LOGGED_ERROR_LENGTH));

                throw new AIUnknownException();
        }
    }

    private const int MAX_LOGGED_ERROR_LENGTH = 2000;

    private static string Truncate ( string value , int maxLength ) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";

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

        if ( string.IsNullOrWhiteSpace(ExtractAnswerText(candidate.Content)) )
            throw new AIInvalidResponseException();
    }

    private static AIResponse BuildResponse (
        GeminiResponse response )
    {
        var candidate = response.Candidates[0];

        return new AIResponse
        {
            Content = ExtractAnswerText(candidate.Content)
        };
    }

    // Gemini 3 may split the answer into several parts, add reasoning ("thought") parts
    // and put a thoughtSignature on a part with empty text: join only the answer text.
    private static string ExtractAnswerText ( GeminiContent content ) =>
        string.Concat(content.Parts
            .Where(part => part.Thought != true && !string.IsNullOrEmpty(part.Text))
            .Select(part => part.Text))
        .Trim();
}