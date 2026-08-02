using MiCash.Domain.Entities;

namespace MiCash.Domain.Services.FinancialChat
{
    public interface IFinancialContextBuilder
    {
        Task<FinancialChatContext> BuildAsync (
            User user ,
            string userMessage ,
            IReadOnlyList<ConversationMessage> history ,
            CancellationToken cancellationToken );
    }
}
