using System.Text.Json;

namespace MiCash.Application.UseCases.WebHook.Handlers
{
    public interface IPagarMeWebhookHandler
    {
        string EventName { get; }

        Task HandleAsync (
            JsonElement payload ,
            CancellationToken cancellationToken );
    }
}
