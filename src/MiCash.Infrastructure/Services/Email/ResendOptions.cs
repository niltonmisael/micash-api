using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace MiCash.Infrastructure.Services.Email
{
    /// <summary>
    /// Section "Resend" of the appsettings.
    /// ApiKey is never versioned: user-secrets in Development, environment variable Resend__ApiKey elsewhere.
    /// </summary>
    public sealed class ResendOptions
    {
        public const string SECTION = "Resend";

        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Sender address; must belong to the domain verified in Resend (mail.micash.com.br).</summary>
        public string FromEmail { get; set; } = string.Empty;

        public string FromName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Runs when the options are first used (first e-mail sent). Messages never include the values.
    /// </summary>
    public sealed class ResendOptionsValidator : IValidateOptions<ResendOptions>
    {
        public ValidateOptionsResult Validate ( string? name , ResendOptions options )
        {
            var failures = new List<string>();

            if ( string.IsNullOrWhiteSpace(options.ApiKey) )
                failures.Add("Resend:ApiKey is required.");

            if ( string.IsNullOrWhiteSpace(options.FromEmail) )
                failures.Add("Resend:FromEmail is required.");
            else if ( MailAddress.TryCreate(options.FromEmail , out var address) == false || address.Address != options.FromEmail.Trim() )
                failures.Add("Resend:FromEmail is not a valid e-mail address.");

            if ( string.IsNullOrWhiteSpace(options.FromName) )
                failures.Add("Resend:FromName is required.");

            return failures.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failures);
        }
    }
}
