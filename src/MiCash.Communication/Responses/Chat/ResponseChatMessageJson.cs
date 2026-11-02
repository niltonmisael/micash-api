namespace MiCash.Communication.Responses.Chat
{
    /// <summary>
    /// The assistant (MI) answer to a message sent by the user.
    /// </summary>
    public class ResponseChatMessageJson
    {
        /// <summary>Id of the assistant message (can be used as the history cursor).</summary>
        public long Id { get; set; }

        /// <summary>Id given to the user's message that was just sent.</summary>
        public long UserMessageId { get; set; }

        public long ConversationId { get; set; }

        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
