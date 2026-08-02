using MiCash.Domain.Entities;

namespace MiCash.Domain.Services.FinancialChat
{
    public interface IConversationMessageService
    {
        Task SaveUserMessageAsync (
            Conversation conversation ,
            string message ,
            CancellationToken cancellationToken );

        Task SaveAssistantMessageAsync (
            Conversation conversation ,
            string message ,
            CancellationToken cancellationToken );

        Task<IReadOnlyList<ConversationMessage>> GetHistoryAsync (
            long conversationId ,
            int quantity ,
            CancellationToken cancellationToken );
    }
}
