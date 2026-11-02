namespace MiCash.Domain.Services.Email
{
    /// <summary>
    /// Sends transactional e-mails. The sender (From) comes from the configuration,
    /// so use cases only say who receives it and what it says.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends one e-mail and returns the id given by the provider.
        /// Throws <see cref="ArgumentException"/> for an invalid message (nothing is sent) and
        /// <see cref="EmailServiceException"/> when the provider fails or refuses it.
        /// </summary>
        Task<string> SendAsync ( EmailMessage message , CancellationToken cancellationToken = default );
    }

    /// <summary>
    /// One e-mail. At least one of <see cref="HtmlBody"/> or <see cref="TextBody"/> is required;
    /// sending both is recommended (clients that do not render HTML show the text).
    /// </summary>
    /// <param name="IdempotencyKey">
    /// Optional. The same key within 24 h does not send the e-mail again, so a retry is safe.
    /// </param>
    public sealed record EmailMessage (
        string To ,
        string Subject ,
        string? HtmlBody = null ,
        string? TextBody = null ,
        string? IdempotencyKey = null );

    public sealed class EmailServiceException : System.Exception
    {
        public EmailServiceException ( string errorCode , bool isTransient , string message , System.Exception? innerException = null )
            : base(message , innerException)
        {
            ErrorCode = errorCode;
            IsTransient = isTransient;
        }

        /// <summary>Provider error type (e.g. "ValidationError", "RateLimitExceeded", "Timeout").</summary>
        public string ErrorCode { get; }

        /// <summary>
        /// True when the same e-mail can be sent again later (network failure, timeout, rate limit, 5xx).
        /// After a timeout the e-mail may already have been sent: use an <see cref="EmailMessage.IdempotencyKey"/> when retrying.
        /// </summary>
        public bool IsTransient { get; }
    }
}
