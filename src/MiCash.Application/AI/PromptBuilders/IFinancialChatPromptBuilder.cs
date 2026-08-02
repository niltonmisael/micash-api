using MiCash.Domain.Services.FinancialChat;
using MiCash.Domain.Services.IA;

namespace MiCash.Application.AI.PromptBuilders
{
    public interface IFinancialChatPromptBuilder
    {
        AIRequest Build ( FinancialChatContext context );
    }
}
