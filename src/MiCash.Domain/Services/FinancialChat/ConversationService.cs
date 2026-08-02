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

            await _unitOfWork.Commit(cancellationToken);

            return conversation;
        }
    }
}
