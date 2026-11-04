using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Conversations
{
    public interface IConversationMessageReadOnlyRepository
    {
        /// <summary>
        /// Most recent messages first (used to build the AI history).
        /// </summary>
        Task<IReadOnlyList<ConversationMessage>> GetLastMessagesAsync (
            long conversationId ,
            int quantity ,
            CancellationToken cancellationToken );

        /// <summary>
        /// Cursor page for the chat screen: up to <paramref name="quantity"/> messages older than
        /// <paramref name="beforeId"/> (or the newest ones when null), newest first.
        /// </summary>
        /// <summary>Messages the user sent (role User), in any of their conversations, at or after <paramref name="sinceUtc"/>.</summary>
        Task<int> CountUserMessagesSinceAsync (
            long userId ,
            DateTime sinceUtc ,
            CancellationToken cancellationToken );

        Task<IReadOnlyList<ConversationMessage>> GetPageAsync (
            long conversationId ,
            long? beforeId ,
            int quantity ,
            CancellationToken cancellationToken );
    }
}
