using System.Text.Json;

namespace MiCash.Application.UseCases.WebHook
{
    public interface IProcessPagarMeWebhookUseCase
    {
        Task ExecuteAsync (
            JsonElement payload ,
            CancellationToken cancellationToken );
    }
}
