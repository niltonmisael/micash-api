using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Conversations;

namespace MiCash.Domain.Services.FinancialChat
{
    public class ConversationMessageService : IConversationMessageService
    {
        private readonly IConversationMessageReadOnlyRepository _readOnlyRepository;
        private readonly IConversationMessageWriteOnlyRepository _writeOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ConversationMessageService (
            IConversationMessageReadOnlyRepository readOnlyRepository ,
            IConversationMessageWriteOnlyRepository writeOnlyRepository ,
            IUnitOfWork unitOfWork )
        {
            _readOnlyRepository = readOnlyRepository;
            _writeOnlyRepository = writeOnlyRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ConversationMessage> SaveUserMessageAsync (
            Conversation conversation ,
            string message ,
            CancellationToken cancellationToken )
        {
            var conversationMessage =
                new ConversationMessage(
                    conversation.Id ,
                    ConversationRole.User ,
                    message);

            await _writeOnlyRepository.AddAsync(
                conversationMessage ,
                cancellationToken);

            return conversationMessage;
        }

        public async Task<ConversationMessage> SaveAssistantMessageAsync (
            Conversation conversation ,
            string message ,
            CancellationToken cancellationToken )
        {
            var conversationMessage =
                new ConversationMessage(
                    conversation.Id ,
                    ConversationRole.Assistant ,
                    message);

            await _writeOnlyRepository.AddAsync(
                conversationMessage ,
                cancellationToken);

            return conversationMessage;
        }

        public async Task<IReadOnlyList<ConversationMessage>> GetHistoryAsync (
            long conversationId ,
            int quantity ,
            CancellationToken cancellationToken )
        {
            return await _readOnlyRepository
                .GetLastMessagesAsync(
                    conversationId ,
                    quantity ,
                    cancellationToken);
        }
    }
}
