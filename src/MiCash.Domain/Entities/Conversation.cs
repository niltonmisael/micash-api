namespace MiCash.Domain.Entities
{
    public class Conversation
    {
        public long Id { get; private set; }

        public long UserId { get; private set; }

        public User User { get; private set; } = default!;

        public string Title { get; private set; } = string.Empty;

        public bool IsActive { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        public ICollection<ConversationMessage> Messages { get; private set; } = [];

        private Conversation ()
        {
        }

        public Conversation (
            long userId ,
            string title )
        {
            UserId = userId;

            Title = title;

            IsActive = true;

            CreatedAt = DateTime.UtcNow;

            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateTitle ( string title )
        {
            Title = title;

            UpdatedAt = DateTime.UtcNow;
        }

        public void Close ()
        {
            IsActive = false;

            UpdatedAt = DateTime.UtcNow;
        }

        public void AddMessage ( ConversationMessage message )
        {
            Messages.Add(message);

            UpdatedAt = DateTime.UtcNow;
        }
    }
}
