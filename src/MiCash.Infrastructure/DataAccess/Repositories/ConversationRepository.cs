using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Conversations;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    public class ConversationRepository : 
        IConversationReadOnlyRepository, 
        IConversationWriteOnlyRepository,
        IConversationMessageReadOnlyRepository,
        IConversationMessageWriteOnlyRepository
    {
        private readonly MiCashDbContext _dbContext;

        public ConversationRepository (MiCashDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync ( Conversation conversation , CancellationToken cancellationToken )
        {
            await _dbContext.AddAsync ( conversation , cancellationToken );
        }

        public async Task AddAsync ( ConversationMessage message , CancellationToken cancellationToken )
        {
            await _dbContext.ConversationMessages.AddAsync ( message , cancellationToken );
        }

        public async Task<Conversation?> GetActiveConversationAsync ( long userId , CancellationToken cancellationToken )
        {
            return await _dbContext.Conversations
                .Where(c => c.IsActive == true)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<IReadOnlyList<ConversationMessage>> GetLastMessagesAsync ( long conversationId , int quantity , CancellationToken cancellationToken )
        {
            return await _dbContext.ConversationMessages
                .Where(cm => cm.ConversationId == conversationId)
                .OrderByDescending(cm => cm.CreatedAt)
                .Take(quantity)
                .ToListAsync();
        }

        public void UpdateAsync ( Conversation conversation )
        {
            _dbContext.Conversations.Update ( conversation );
        }
    }
}
