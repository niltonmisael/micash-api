using MiCash.Domain.Entities;

namespace MiCash.Domain.Services.FinancialChat
{
    public interface IConversationMessageService
    {
        Task<ConversationMessage> SaveUserMessageAsync (
            Conversation conversation ,
            string message ,
            CancellationToken cancellationToken );

        Task<ConversationMessage> SaveAssistantMessageAsync (
            Conversation conversation ,
            string message ,
            CancellationToken cancellationToken );

        Task<IReadOnlyList<ConversationMessage>> GetHistoryAsync (
            long conversationId ,
            int quantity ,
            CancellationToken cancellationToken );
    }
}
