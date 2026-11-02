using MiCash.Domain.Entities;

namespace MiCash.Domain.Services.FinancialChat
{
    public interface IConversationService
    {
        Task<Conversation?> GetActiveConversationAsync (
            User user ,
            CancellationToken cancellationToken );

        Task<Conversation> GetOrCreateConversationAsync (
            User user ,
            CancellationToken cancellationToken );

        /// <summary>
        /// Closes the current conversation (if any) and opens a new, empty one.
        /// </summary>
        Task<Conversation> StartNewConversationAsync (
            User user ,
            CancellationToken cancellationToken );
    }
}
