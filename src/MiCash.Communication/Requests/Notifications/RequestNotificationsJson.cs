namespace MiCash.Communication.Requests.Notifications
{
    public class RequestNotificationsJson
    {
        /// <summary>Cursor: return notifications older than this notification id. Omit for the newest page.</summary>
        public long? Before { get; set; }

        /// <summary>Page size (1 to 50, default 20).</summary>
        public int Limit { get; set; } = 20;
    }
}
