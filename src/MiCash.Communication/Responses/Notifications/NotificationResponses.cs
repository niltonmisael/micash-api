namespace MiCash.Communication.Responses.Notifications
{
    public class ResponseNotificationsJson
    {
        /// <summary>Newest first.</summary>
        public List<ResponseNotificationJson> Notifications { get; set; } = [];

        /// <summary>True when there are older notifications: request again with before = id of the last one.</summary>
        public bool HasMore { get; set; }
    }

    public class ResponseNotificationJson
    {
        public long Id { get; set; }

        /// <summary>What produced it (e.g. "test"); lets the app pick an icon.</summary>
        public string Type { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        /// <summary>Internal app route to open; null = just open the app.</summary>
        public string? Url { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>null = unread.</summary>
        public DateTime? ReadAt { get; set; }
    }

    public class ResponseUnreadNotificationsCountJson
    {
        public int Count { get; set; }
    }
}
