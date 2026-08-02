using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Conversations
{
    public interface IConversationWriteOnlyRepository
    {
        Task AddAsync (
            Conversation conversation ,
            CancellationToken cancellationToken );

        void UpdateAsync (
            Conversation conversation );
    }
}
