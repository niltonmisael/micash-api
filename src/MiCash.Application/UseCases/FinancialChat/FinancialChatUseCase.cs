using MiCash.Application.AI.PromptBuilders;
using MiCash.Communication.Requests.Chat;
using MiCash.Communication.Responses.Chat;
using MiCash.Domain.Repositories;
using MiCash.Domain.Services.FinancialChat;
using MiCash.Domain.Services.IA;
using MiCash.Domain.Services.LoggedUser;

namespace MiCash.Application.UseCases.FinancialChat
{
    public class FinancialChatUseCase : IFinancialChatUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IConversationService _conversationService;
        private readonly IConversationMessageService _conversationMessageService;
        private readonly IFinancialContextBuilder _financialContextBuilder;
        private readonly IFinancialChatPromptBuilder _financialChatPromptBuilder;
        private readonly IAIService _aiService;
        private readonly IUnitOfWork _unitOfWork;

        public FinancialChatUseCase (
            ILoggedUser loggedUser ,
            IConversationService conversationService ,
            IConversationMessageService conversationMessageService ,
            IFinancialContextBuilder financialContextBuilder ,
            IFinancialChatPromptBuilder financialChatPromptBuilder ,
            IAIService aiService ,
            IUnitOfWork unitOfWork )
        {
            _loggedUser = loggedUser;
            _conversationService = conversationService;
            _conversationMessageService = conversationMessageService;
            _financialContextBuilder = financialContextBuilder;
            _financialChatPromptBuilder = financialChatPromptBuilder;
            _aiService = aiService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseChatMessageJson> SendChatMessageAsync (
            RequestChatMessageJson chatMessage ,
            CancellationToken cancellationToken )
        {
            var user =
                await _loggedUser.Get();

            var conversation =
                await _conversationService.GetOrCreateConversationAsync(
                    user ,
                    cancellationToken);

            var history =
                await _conversationMessageService.GetHistoryAsync(
                    conversation.Id ,
                    10 ,
                    cancellationToken);

            var context =
                await _financialContextBuilder.BuildAsync(
                    user ,
                    chatMessage.Message ,
                    history ,
                    cancellationToken);

            var aiRequest =
                _financialChatPromptBuilder.Build(context);

            await _conversationMessageService.SaveUserMessageAsync(
                conversation ,
                chatMessage.Message ,
                cancellationToken);

            var aiResponse =
                await _aiService.GenerateResponseAsync(
                    aiRequest ,
                    cancellationToken);

            await _conversationMessageService.SaveAssistantMessageAsync(
                conversation ,
                aiResponse.Content ,
                cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            return new ResponseChatMessageJson
            {
                Message = aiResponse.Content
            };
        }
    }
}

