using System.Text.Json;

namespace MiCash.Application.UseCases.WebHook.Handlers
{
    public interface IPagarMeWebhookHandler
    {
        string EventName { get; }

        /// <summary>
        /// Applies the event to the tracked entities. Must NOT commit: <see cref="ProcessPagarMeWebhookUseCase"/>
        /// commits the changes and the webhook log in a single transaction.
        /// Throw <see cref="WebhookNotReadyException"/> to have Pagar.me deliver the event again later.
        /// </summary>
        Task HandleAsync (
            JsonElement payload ,
            CancellationToken cancellationToken );
    }
}
