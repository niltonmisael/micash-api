using MiCash.Domain.Entities;

namespace MiCash.Domain.Services.FinancialChat
{
    public interface IConversationService
    {
        Task<Conversation> GetOrCreateConversationAsync (
            User user ,
            CancellationToken cancellationToken );
    }
}
