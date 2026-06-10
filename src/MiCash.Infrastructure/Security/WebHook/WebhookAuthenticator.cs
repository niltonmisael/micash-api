using MiCash.Domain.Security.WebHook;
using MiCash.Infrastructure.Services.Payments;
using Microsoft.Extensions.Options;
using System.Text;

namespace MiCash.Infrastructure.Security.WebHook
{
    public class WebhookAuthenticator : IWebhookAuthenticator
    {
        private readonly WebhookSettings _settings;

        public WebhookAuthenticator (
            IOptions<WebhookSettings> settings)
        {
            _settings = settings.Value;
        }
        public bool IsValid ( string? authorizationHeader )
        {
            if ( string.IsNullOrWhiteSpace(authorizationHeader) )
                return false;

            if ( !authorizationHeader.StartsWith("Basic ") )
                return false;

            try {
                var encodedCredentials =
                    authorizationHeader["Basic ".Length..];

                var credentials =
                    Encoding.UTF8.GetString(
                        Convert.FromBase64String(encodedCredentials));

                var parts = credentials.Split(':' , 2);

                if ( parts.Length != 2 )
                    return false;

                var username = parts[0];
                var password = parts[1];

                return username == _settings.Username
                    && password == _settings.Password;
            } catch {
                return false;
            }
        }
    }
}
