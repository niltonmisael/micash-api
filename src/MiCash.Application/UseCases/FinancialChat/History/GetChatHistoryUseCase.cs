using MiCash.Communication.Requests.Chat;
using MiCash.Communication.Responses.Chat;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.Conversations;
using MiCash.Domain.Services.FinancialChat;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.FinancialChat.History
{
    public interface IGetChatHistoryUseCase
    {
        Task<ResponseChatHistoryJson> Execute ( RequestChatHistoryJson request , CancellationToken cancellationToken );
    }

    public class GetChatHistoryUseCase : IGetChatHistoryUseCase
    {
        public const int DEFAULT_LIMIT = 30;
        public const int MAX_LIMIT = 100;

        private readonly ILoggedUser _loggedUser;
        private readonly IConversationService _conversationService;
        private readonly IConversationMessageReadOnlyRepository _messageReadOnlyRepository;

        public GetChatHistoryUseCase (
            ILoggedUser loggedUser ,
            IConversationService conversationService ,
            IConversationMessageReadOnlyRepository messageReadOnlyRepository )
        {
            _loggedUser = loggedUser;
            _conversationService = conversationService;
            _messageReadOnlyRepository = messageReadOnlyRepository;
        }

        public async Task<ResponseChatHistoryJson> Execute ( RequestChatHistoryJson request , CancellationToken cancellationToken )
        {
            var limit = request.Limit <= 0 ? DEFAULT_LIMIT : Math.Min(request.Limit , MAX_LIMIT);

            var user = await _loggedUser.Get();

            // Read-only: never creates a conversation just because the screen was opened.
            var conversation = await _conversationService.GetActiveConversationAsync(user , cancellationToken);

            if ( conversation is null ) {
                return new ResponseChatHistoryJson();
            }

            // Fetch one extra row to know whether there is an older page.
            var page = await _messageReadOnlyRepository.GetPageAsync(
                conversation.Id ,
                request.Before ,
                limit + 1 ,
                cancellationToken);

            return new ResponseChatHistoryJson
            {
                ConversationId = conversation.Id ,
                HasMore = page.Count > limit ,
                Messages = page
                    .Take(limit)
                    .OrderBy(message => message.Id)
                    .Select(message => new ResponseChatHistoryMessageJson
                    {
                        Id = message.Id ,
                        Role = message.Role switch
                        {
                            ConversationRole.User => "user" ,
                            ConversationRole.Assistant => "assistant" ,
                            _ => "system"
                        } ,
                        Content = message.Content ,
                        CreatedAt = message.CreatedAt
                    })
                    .ToList()
            };
        }
    }
}
