namespace MiCash.Communication.Requests.Chat
{
    public class RequestChatHistoryJson
    {
        /// <summary>Cursor: return messages older than this message id. Omit for the newest page.</summary>
        public long? Before { get; set; }

        /// <summary>Page size (1 to 100, default 30).</summary>
        public int Limit { get; set; } = 30;
    }
}
