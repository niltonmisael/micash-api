using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Conversations;

namespace MiCash.Domain.Services.FinancialChat
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationReadOnlyRepository _readOnlyRepository;

        private readonly IConversationWriteOnlyRepository _writeOnlyRepository;

        private readonly IUnitOfWork _unitOfWork;

        public ConversationService (
            IConversationReadOnlyRepository readOnlyRepository ,
            IConversationWriteOnlyRepository writeOnlyRepository ,
            IUnitOfWork unitOfWork )
        {
            _readOnlyRepository = readOnlyRepository;
            _writeOnlyRepository = writeOnlyRepository;
            _unitOfWork = unitOfWork;
        }

        public Task<Conversation?> GetActiveConversationAsync (
            User user ,
            CancellationToken cancellationToken )
        {
            return _readOnlyRepository.GetActiveConversationAsync(user.Id , cancellationToken);
        }

        public async Task<Conversation> GetOrCreateConversationAsync (
            User user ,
            CancellationToken cancellationToken )
        {
            var conversation =
                await _readOnlyRepository
                    .GetActiveConversationAsync(
                        user.Id ,
                        cancellationToken);

            if ( conversation is not null )
                return conversation;

            conversation =
                new Conversation(
                    user.Id ,
                    string.Empty);

            await _writeOnlyRepository.AddAsync(
                conversation ,
                cancellationToken);

            // A concurrent first message violates the unique "one active conversation" index;
            // the caller (application layer) handles that conflict.
            await _unitOfWork.Commit(cancellationToken);

            return conversation;
        }

        public async Task<Conversation> StartNewConversationAsync (
            User user ,
            CancellationToken cancellationToken )
        {
            var current =
                await _readOnlyRepository
                    .GetActiveConversationAsync(
                        user.Id ,
                        cancellationToken);

            current?.Close();

            var conversation =
                new Conversation(
                    user.Id ,
                    string.Empty);

            await _writeOnlyRepository.AddAsync(
                conversation ,
                cancellationToken);

            // EF orders the UPDATE (close) before the INSERT, so the unique index is respected;
            // a concurrent "new conversation" surfaces as ConflictException (409).
            await _unitOfWork.Commit(cancellationToken);

            return conversation;
        }
    }
}
