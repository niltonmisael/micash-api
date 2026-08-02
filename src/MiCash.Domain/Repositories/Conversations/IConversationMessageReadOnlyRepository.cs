using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Conversations
{
    public interface IConversationMessageReadOnlyRepository
    {
        Task<IReadOnlyList<ConversationMessage>> GetLastMessagesAsync (
            long conversationId ,
            int quantity ,
            CancellationToken cancellationToken );
    }
}
