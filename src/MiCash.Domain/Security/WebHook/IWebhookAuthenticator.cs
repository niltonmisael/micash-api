namespace MiCash.Domain.Security.WebHook
{
    public interface IWebhookAuthenticator
    {
        bool IsValid ( string? authorizationHeader );
    }
}
