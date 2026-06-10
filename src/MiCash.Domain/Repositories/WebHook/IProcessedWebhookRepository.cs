using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.WebHook
{
    public interface IProcessedWebhookRepository
    {
        Task AddAsync ( ProcessedWebhook webhookLog , CancellationToken cancellationToken );
        Task<bool> ExistsByWebhookIdAsync ( string webhookId , CancellationToken cancellationToken );
    }
}
