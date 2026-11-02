using MiCash.Domain.Services.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;
using System.Net;
using System.Net.Mail;
using EmailMessage = MiCash.Domain.Services.Email.EmailMessage;

namespace MiCash.Infrastructure.Services.Email
{
    /// <summary>
    /// <see cref="IEmailService"/> on top of the official Resend SDK. The SDK stays inside this class:
    /// its errors are converted to <see cref="EmailServiceException"/>.
    /// Logs never include the API key, the subject, the body or the full recipient address
    /// (e-mails may carry codes or links that grant access).
    /// </summary>
    public sealed class ResendEmailService : IEmailService
    {
        private const string TIMEOUT = "Timeout";

        private readonly IResend _resend;
        private readonly ResendOptions _options;
        private readonly ILogger<ResendEmailService> _logger;

        public ResendEmailService ( IResend resend , IOptions<ResendOptions> options , ILogger<ResendEmailService> logger )
        {
            _resend = resend;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> SendAsync ( EmailMessage message , CancellationToken cancellationToken = default )
        {
            Validate(message);

            var email = new Resend.EmailMessage
            {
                From = new EmailAddress { Email = _options.FromEmail , DisplayName = _options.FromName } ,
                To = message.To.Trim() ,
                Subject = message.Subject ,
                HtmlBody = message.HtmlBody ,
                TextBody = message.TextBody
            };

            var recipient = MaskRecipient(message.To);

            try {
                var response = string.IsNullOrWhiteSpace(message.IdempotencyKey)
                    ? await _resend.EmailSendAsync(email , cancellationToken)
                    : await _resend.EmailSendAsync(message.IdempotencyKey , email , cancellationToken);

                // ThrowExceptions is on, but do not rely on it.
                if ( response.Success == false )
                    throw response.Exception ?? new ResendException(null , ErrorType.MissingResponse , "Missing response" , null);

                var id = response.Content.ToString();

                _logger.LogInformation("E-mail sent via Resend. EmailId: {EmailId}, Recipient: {Recipient}" , id , recipient);

                return id;

            } catch ( ResendException ex ) {

                var isTransient = IsTransient(ex);

                // ex.Message is the provider's text and may echo the recipient: only the type and status are logged.
                _logger.LogWarning(
                    "Resend refused or failed to send an e-mail. ErrorType: {ErrorType}, StatusCode: {StatusCode}, Transient: {IsTransient}, Recipient: {Recipient}" ,
                    ex.ErrorType , (int?)ex.StatusCode , isTransient , recipient);

                throw new EmailServiceException(ex.ErrorType.ToString() , isTransient , $"Failed to send e-mail ({ex.ErrorType})." , ex);

            } catch ( OperationCanceledException ex ) when ( cancellationToken.IsCancellationRequested == false ) {

                // HttpClient timeout: Resend may or may not have accepted the e-mail.
                _logger.LogWarning("Timeout while sending an e-mail via Resend. Recipient: {Recipient}" , recipient);

                throw new EmailServiceException(TIMEOUT , isTransient: true , "Timeout while sending e-mail." , ex);
            }
        }

        private static void Validate ( EmailMessage message )
        {
            ArgumentNullException.ThrowIfNull(message);

            if ( string.IsNullOrWhiteSpace(message.To)
                || MailAddress.TryCreate(message.To.Trim() , out var address) == false
                || address.Address != message.To.Trim() )
                throw new ArgumentException("A single valid recipient address is required." , nameof(message));

            if ( string.IsNullOrWhiteSpace(message.Subject) )
                throw new ArgumentException("The subject is required." , nameof(message));

            if ( string.IsNullOrWhiteSpace(message.HtmlBody) && string.IsNullOrWhiteSpace(message.TextBody) )
                throw new ArgumentException("An HTML or text body is required." , nameof(message));
        }

        // The SDK marks only network failures and rate/quota limits as transient; 429 and 5xx are too.
        private static bool IsTransient ( ResendException ex ) =>
            ex.IsTransient
            || ex.StatusCode == HttpStatusCode.TooManyRequests
            || (int?)ex.StatusCode >= 500;

        /// <summary>"joao.silva@gmail.com" → "j***@gmail.com".</summary>
        private static string MaskRecipient ( string email )
        {
            var value = email.Trim();
            var at = value.LastIndexOf('@');

            return at <= 0
                ? "***"
                : $"{value[0]}***{value[at..]}";
        }
    }
}
