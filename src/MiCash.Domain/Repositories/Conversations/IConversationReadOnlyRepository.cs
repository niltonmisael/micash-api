using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Conversations
{
    public interface IConversationReadOnlyRepository
    {
        Task<Conversation?> GetActiveConversationAsync (
            long userId ,
            CancellationToken cancellationToken );

    }
}
