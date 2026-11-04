using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
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

        public ConversationRepository ( MiCashDbContext dbContext )
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync ( Conversation conversation , CancellationToken cancellationToken )
        {
            await _dbContext.Conversations.AddAsync(conversation , cancellationToken);
        }

        public async Task AddAsync ( ConversationMessage message , CancellationToken cancellationToken )
        {
            await _dbContext.ConversationMessages.AddAsync(message , cancellationToken);
        }

        public Task<Conversation?> GetActiveConversationAsync ( long userId , CancellationToken cancellationToken )
        {
            // Tracked on purpose: the caller may close it.
            return _dbContext.Conversations
                .Where(c => c.UserId == userId && c.IsActive)
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ConversationMessage>> GetLastMessagesAsync ( long conversationId , int quantity , CancellationToken cancellationToken )
        {
            // Id is monotonic; CreatedAt can tie for messages saved in the same request.
            return await _dbContext.ConversationMessages
                .AsNoTracking()
                .Where(cm => cm.ConversationId == conversationId)
                .OrderByDescending(cm => cm.Id)
                .Take(quantity)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ConversationMessage>> GetPageAsync ( long conversationId , long? beforeId , int quantity , CancellationToken cancellationToken )
        {
            var query = _dbContext.ConversationMessages
                .AsNoTracking()
                .Where(cm => cm.ConversationId == conversationId);

            if ( beforeId is not null ) {
                query = query.Where(cm => cm.Id < beforeId.Value);
            }

            return await query
                .OrderByDescending(cm => cm.Id)
                .Take(quantity)
                .ToListAsync(cancellationToken);
        }

        public Task<int> CountUserMessagesSinceAsync ( long userId , DateTime sinceUtc , CancellationToken cancellationToken )
        {
            return _dbContext.ConversationMessages
                .AsNoTracking()
                .Where(cm =>
                    cm.Conversation.UserId == userId &&
                    cm.Role == ConversationRole.User &&
                    cm.CreatedAt >= sinceUtc)
                .CountAsync(cancellationToken);
        }

        public void UpdateAsync ( Conversation conversation )
        {
            _dbContext.Conversations.Update(conversation);
        }
    }
}
