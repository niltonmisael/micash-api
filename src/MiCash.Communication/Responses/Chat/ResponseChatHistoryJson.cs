namespace MiCash.Communication.Responses.Chat
{
    public class ResponseChatHistoryJson
    {
        /// <summary>Active conversation id; null when the user has never chatted (or just started a new one and sent nothing).</summary>
        public long? ConversationId { get; set; }

        /// <summary>Messages in chronological order (oldest first), ready to render.</summary>
        public List<ResponseChatHistoryMessageJson> Messages { get; set; } = [];

        /// <summary>True when there are older messages: request again with before = Messages[0].Id.</summary>
        public bool HasMore { get; set; }
    }

    public class ResponseChatHistoryMessageJson
    {
        public long Id { get; set; }

        /// <summary>"user" or "assistant".</summary>
        public string Role { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
