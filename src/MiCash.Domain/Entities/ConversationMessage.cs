using MiCash.Domain.Enums;

namespace MiCash.Domain.Entities
{
    public class ConversationMessage
    {
        public long Id { get; private set; }

        public long ConversationId { get; private set; }

        public Conversation Conversation { get; private set; } = default!;

        public ConversationRole Role { get; private set; }

        public string Content { get; private set; } = string.Empty;

        public DateTime CreatedAt { get; private set; }

        private ConversationMessage ()
        {
        }

        public ConversationMessage (
            long conversationId ,
            ConversationRole role ,
            string content )
        {
            ConversationId = conversationId;

            Role = role;

            Content = content;

            CreatedAt = DateTime.UtcNow;
        }
    }
}
