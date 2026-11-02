using MiCash.Domain.Services.Email;

namespace MiCash.Application.Services.Email
{
    /// <summary>
    /// Renders the template of the model and hands the result to <see cref="IEmailService"/> (Resend).
    /// Errors are not swallowed: <see cref="ArgumentException"/> for an invalid model or recipient,
    /// <see cref="EmailServiceException"/> when the provider fails (see IsTransient).
    /// </summary>
    public sealed class TransactionalEmailService : ITransactionalEmailService
    {
        private readonly IEmailTemplateRenderer _renderer;
        private readonly IEmailService _emailService;

        public TransactionalEmailService ( IEmailTemplateRenderer renderer , IEmailService emailService )
        {
            _renderer = renderer;
            _emailService = emailService;
        }

        public Task<string> SendAsync ( string to , TransactionalEmail email , string? idempotencyKey = null , CancellationToken cancellationToken = default )
        {
            var rendered = _renderer.Render(email);

            return _emailService.SendAsync(
                new EmailMessage(to , rendered.Subject , rendered.HtmlBody , rendered.TextBody , idempotencyKey) ,
                cancellationToken);
        }
    }
}
