using MiCash.Communication.Requests.Chat;
using MiCash.Communication.Responses.Chat;

namespace MiCash.Application.UseCases.FinancialChat
{
    public interface IFinancialChatUseCase
    {
        Task<ResponseChatMessageJson> SendChatMessageAsync(RequestChatMessageJson chatMessage, CancellationToken cancellationToken);
    }
}
