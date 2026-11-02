using MiCash.Communication.Responses.Chat;
using MiCash.Domain.Services.FinancialChat;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.FinancialChat.StartConversation
{
    public interface IStartConversationUseCase
    {
        Task<ResponseStartedConversationJson> Execute ( CancellationToken cancellationToken );
    }

    /// <summary>
    /// "Nova conversa": closes the current conversation (its messages are kept in the database)
    /// and opens an empty one, so the MI no longer receives the previous history.
    /// </summary>
    public class StartConversationUseCase : IStartConversationUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IConversationService _conversationService;

        public StartConversationUseCase (
            ILoggedUser loggedUser ,
            IConversationService conversationService )
        {
            _loggedUser = loggedUser;
            _conversationService = conversationService;
        }

        public async Task<ResponseStartedConversationJson> Execute ( CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            var conversation = await _conversationService.StartNewConversationAsync(user , cancellationToken);

            return new ResponseStartedConversationJson
            {
                ConversationId = conversation.Id
            };
        }
    }
}
