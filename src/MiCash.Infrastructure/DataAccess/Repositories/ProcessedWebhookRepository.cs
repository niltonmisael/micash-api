using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.WebHook;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    public sealed class ProcessedWebhookRepository : IProcessedWebhookRepository
    {
        private readonly MiCashDbContext _dbContext;

        public ProcessedWebhookRepository (
            MiCashDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync ( ProcessedWebhook webhookLog , CancellationToken cancellationToken )
        {
            await _dbContext.AddAsync ( webhookLog , cancellationToken);
        }

        public async Task<bool> ExistsByWebhookIdAsync ( string webhookId , CancellationToken cancellationToken )
        {
            return await _dbContext.ProcessedWebhooks.AnyAsync(w  => w.WebhookId == webhookId, cancellationToken);
        }
    }
}
