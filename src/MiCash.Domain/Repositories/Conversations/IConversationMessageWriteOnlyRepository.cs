using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Conversations
{
    public interface IConversationMessageWriteOnlyRepository
    {
        Task AddAsync (
            ConversationMessage message ,
            CancellationToken cancellationToken );
    }
}
