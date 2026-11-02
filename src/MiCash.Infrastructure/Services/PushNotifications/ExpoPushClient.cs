using MiCash.Domain.Services.PushNotifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiCash.Infrastructure.Services.PushNotifications
{
    /// <summary>
    /// Typed HttpClient for the Expo Push Service. No HTTP-level retry: a resent "send" may duplicate
    /// notifications, so failures are classified (<see cref="PushFailureKind"/>) and retried by the jobs.
    /// Tokens are never logged in full.
    /// </summary>
    public sealed class ExpoPushClient : IExpoPushClient
    {
        public const string SEND_PATH = "/--/api/v2/push/send";
        public const string RECEIPTS_PATH = "/--/api/v2/push/getReceipts";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _httpClient;
        private readonly ExpoPushSettings _settings;
        private readonly ILogger<ExpoPushClient> _logger;

        public ExpoPushClient (
            HttpClient httpClient ,
            IOptions<ExpoPushSettings> settings ,
            ILogger<ExpoPushClient> logger )
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ExpoPushTicket>> SendAsync (
            IReadOnlyList<ExpoPushMessage> messages ,
            CancellationToken cancellationToken )
        {
            if ( messages.Count == 0 ) {
                return [];
            }

            if ( messages.Count > IExpoPushClient.MAX_MESSAGES_PER_REQUEST ) {
                throw new ArgumentException(
                    $"Expo accepts at most {IExpoPushClient.MAX_MESSAGES_PER_REQUEST} messages per request." , nameof(messages));
            }

            var payload = messages.Select(ToPayload).ToList();

            var body = await PostAsync<ExpoSendResponse>(SEND_PATH , payload , isSend: true , cancellationToken);

            if ( body?.Data is null || body.Data.Count != messages.Count ) {
                // Expo processed *something* but we can't tell which ticket belongs to which message.
                _logger.LogError(
                    "Expo send: resposta inesperada ({Received} tickets para {Sent} mensagens). Erros: {Errors}" ,
                    body?.Data?.Count , messages.Count , DescribeErrors(body?.Errors));

                throw new PushServiceException(PushFailureKind.Uncertain , "Unexpected Expo send response.");
            }

            return body.Data
                .Select(ticket => ticket.Status == "ok" && !string.IsNullOrEmpty(ticket.Id)
                    ? new ExpoPushTicket(true , ticket.Id , null , null)
                    : new ExpoPushTicket(false , null , ticket.Details?.Error ?? "Unknown" , PushTokenMask.Sanitize(ticket.Message)))
                .ToList();
        }

        public async Task<IReadOnlyDictionary<string , ExpoPushReceipt>> GetReceiptsAsync (
            IReadOnlyList<string> ticketIds ,
            CancellationToken cancellationToken )
        {
            if ( ticketIds.Count == 0 ) {
                return new Dictionary<string , ExpoPushReceipt>();
            }

            if ( ticketIds.Count > IExpoPushClient.MAX_RECEIPT_IDS_PER_REQUEST ) {
                throw new ArgumentException(
                    $"Expo accepts at most {IExpoPushClient.MAX_RECEIPT_IDS_PER_REQUEST} ids per request." , nameof(ticketIds));
            }

            var body = await PostAsync<ExpoReceiptsResponse>(RECEIPTS_PATH , new { ids = ticketIds } , isSend: false , cancellationToken);

            if ( body?.Data is null ) {
                _logger.LogError("Expo getReceipts: resposta sem dados. Erros: {Errors}" , DescribeErrors(body?.Errors));

                throw new PushServiceException(PushFailureKind.Retryable , "Unexpected Expo receipts response.");
            }

            return body.Data.ToDictionary(
                pair => pair.Key ,
                pair => pair.Value.Status == "ok"
                    ? new ExpoPushReceipt(true , null , null)
                    : new ExpoPushReceipt(false , pair.Value.Details?.Error ?? "Unknown" , PushTokenMask.Sanitize(pair.Value.Message)));
        }

        private ExpoMessagePayload ToPayload ( ExpoPushMessage message ) => new()
        {
            To = message.To ,
            Title = message.Title ,
            Body = message.Body ,
            Data = message.Data ,
            Sound = "default" ,
            Priority = "high" ,
            ChannelId = string.IsNullOrWhiteSpace(_settings.ChannelId) ? null : _settings.ChannelId
        };

        private async Task<T?> PostAsync<T> ( string path , object payload , bool isSend , CancellationToken cancellationToken )
        {
            var operation = isSend ? "send" : "getReceipts";

            using var request = new HttpRequestMessage(HttpMethod.Post , path)
            {
                Content = JsonContent.Create(payload , options: JsonOptions)
            };

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if ( !string.IsNullOrWhiteSpace(_settings.AccessToken) ) {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer" , _settings.AccessToken);
            }

            HttpResponseMessage response;

            try {
                response = await _httpClient.SendAsync(request , cancellationToken);
            } catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
                throw;
            } catch ( BrokenCircuitException ex ) {
                _logger.LogWarning(ex , "Expo {Operation}: circuit breaker aberto, requisição não enviada." , operation);

                throw new PushServiceException(PushFailureKind.Retryable , "Expo circuit open." , ex);
            } catch ( HttpRequestException ex ) when ( IsNotSent(ex) ) {
                _logger.LogWarning(ex , "Expo {Operation}: falha de conexão ({Error})." , operation , ex.HttpRequestError);

                throw new PushServiceException(PushFailureKind.Retryable , "Could not connect to Expo." , ex);
            } catch ( System.Exception ex ) when ( ex is HttpRequestException or OperationCanceledException ) {
                // Timeout or connection lost after the request left: Expo may have processed it.
                _logger.LogError(ex , "Expo {Operation}: timeout ou conexão perdida após o envio." , operation);

                throw new PushServiceException(
                    isSend ? PushFailureKind.Uncertain : PushFailureKind.Retryable , "Expo request interrupted." , ex);
            }

            using ( response ) {
                if ( !response.IsSuccessStatusCode ) {
                    var detail = PushTokenMask.Sanitize(await ReadSafeAsync(response , cancellationToken));

                    var kind = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500
                        ? PushFailureKind.Retryable
                        : PushFailureKind.Rejected;

                    _logger.LogError(
                        "Expo {Operation}: HTTP {StatusCode} ({Kind}). Detalhe: {Detail}" ,
                        operation , (int)response.StatusCode , kind , detail);

                    throw new PushServiceException(kind , $"Expo returned HTTP {(int)response.StatusCode}.");
                }

                try {
                    return await response.Content.ReadFromJsonAsync<T>(JsonOptions , cancellationToken);
                } catch ( JsonException ex ) {
                    _logger.LogError(ex , "Expo {Operation}: JSON inválido na resposta." , operation);

                    throw new PushServiceException(
                        isSend ? PushFailureKind.Uncertain : PushFailureKind.Retryable , "Invalid Expo response." , ex);
                }
            }
        }

        /// <summary>Failures where the request certainly never reached Expo.</summary>
        private static bool IsNotSent ( HttpRequestException ex ) => ex.HttpRequestError is
            HttpRequestError.NameResolutionError or
            HttpRequestError.ConnectionError or
            HttpRequestError.SecureConnectionError or
            HttpRequestError.ProxyTunnelError;

        private static async Task<string> ReadSafeAsync ( HttpResponseMessage response , CancellationToken cancellationToken )
        {
            try {
                var text = await response.Content.ReadAsStringAsync(cancellationToken);

                return text.Length > 500 ? text[..500] : text;
            } catch {
                return string.Empty;
            }
        }

        private static string DescribeErrors ( List<ExpoRequestError>? errors ) =>
            errors is null or { Count: 0 }
                ? "-"
                : string.Join("; " , errors.Select(e => $"{e.Code}: {PushTokenMask.Sanitize(e.Message)}"));

        private sealed class ExpoMessagePayload
        {
            public string To { get; init; } = string.Empty;
            public string Title { get; init; } = string.Empty;
            public string Body { get; init; } = string.Empty;
            public IReadOnlyDictionary<string , string>? Data { get; init; }
            public string? Sound { get; init; }
            public string? Priority { get; init; }
            public string? ChannelId { get; init; }
        }

        private sealed class ExpoSendResponse
        {
            public List<ExpoStatusItem>? Data { get; set; }
            public List<ExpoRequestError>? Errors { get; set; }
        }

        private sealed class ExpoReceiptsResponse
        {
            public Dictionary<string , ExpoStatusItem>? Data { get; set; }
            public List<ExpoRequestError>? Errors { get; set; }
        }

        private sealed class ExpoStatusItem
        {
            public string? Status { get; set; }
            public string? Id { get; set; }
            public string? Message { get; set; }
            public ExpoErrorDetails? Details { get; set; }
        }

        private sealed class ExpoErrorDetails
        {
            public string? Error { get; set; }
        }

        private sealed class ExpoRequestError
        {
            public string? Code { get; set; }
            public string? Message { get; set; }
        }
    }
}
