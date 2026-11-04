using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.WebHook
{
    public interface IProcessedWebhookRepository
    {
        Task AddAsync ( ProcessedWebhook webhookLog , CancellationToken cancellationToken );

        /// <summary>Tracked log of a webhook delivery (one row per webhook id), or null when never received.</summary>
        Task<ProcessedWebhook?> GetByWebhookIdAsync ( string webhookId , CancellationToken cancellationToken );

        void Update ( ProcessedWebhook webhookLog );
    }
}
