namespace MiCash.Domain.Services.PushNotifications
{
    /// <summary>Expo Push Service (https://docs.expo.dev/push-notifications/sending-notifications/).</summary>
    public interface IExpoPushClient
    {
        /// <summary>Expo limit of messages per send request.</summary>
        public const int MAX_MESSAGES_PER_REQUEST = 100;

        /// <summary>Expo limit of ticket ids per getReceipts request.</summary>
        public const int MAX_RECEIPT_IDS_PER_REQUEST = 1000;

        /// <summary>
        /// Sends up to <see cref="MAX_MESSAGES_PER_REQUEST"/> messages in one request. Returns one ticket
        /// per message, in the same order. Throws <see cref="PushServiceException"/> when the request as a whole fails.
        /// </summary>
        Task<IReadOnlyList<ExpoPushTicket>> SendAsync ( IReadOnlyList<ExpoPushMessage> messages , CancellationToken cancellationToken );

        /// <summary>
        /// Receipts for up to <see cref="MAX_RECEIPT_IDS_PER_REQUEST"/> ticket ids. Ids without a receipt yet
        /// (or expired) are absent from the result.
        /// </summary>
        Task<IReadOnlyDictionary<string , ExpoPushReceipt>> GetReceiptsAsync ( IReadOnlyList<string> ticketIds , CancellationToken cancellationToken );
    }

    public sealed record ExpoPushMessage (
        string To ,
        string Title ,
        string Body ,
        IReadOnlyDictionary<string , string> Data );

    /// <summary>Ticket of one message: <see cref="Id"/> when ok, or an error (e.g. DeviceNotRegistered).</summary>
    public sealed record ExpoPushTicket ( bool IsOk , string? Id , string? ErrorCode , string? ErrorMessage );

    public sealed record ExpoPushReceipt ( bool IsOk , string? ErrorCode , string? ErrorMessage );

    /// <summary>Expo error codes the backend reacts to.</summary>
    public static class ExpoPushErrors
    {
        /// <summary>The token is no longer valid (app uninstalled, token rotated): deactivate the device.</summary>
        public const string DEVICE_NOT_REGISTERED = "DeviceNotRegistered";

        /// <summary>Too many messages to the same device: temporary, retry later.</summary>
        public const string MESSAGE_RATE_EXCEEDED = "MessageRateExceeded";
    }

    public enum PushFailureKind
    {
        /// <summary>Request certainly not processed (connection not established, 429, 5xx, circuit open): safe to retry.</summary>
        Retryable,

        /// <summary>Request may have reached Expo (timeout / connection lost after sending): never resend.</summary>
        Uncertain,

        /// <summary>Expo rejected the request (4xx, invalid credentials, malformed response): retrying won't help.</summary>
        Rejected
    }

    public sealed class PushServiceException : System.Exception
    {
        public PushServiceException ( PushFailureKind kind , string message , System.Exception? innerException = null )
            : base(message , innerException)
        {
            Kind = kind;
        }

        public PushFailureKind Kind { get; }
    }
}
