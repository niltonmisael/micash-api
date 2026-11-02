namespace MiCash.Infrastructure.Services.PushNotifications
{
    /// <summary>Section "ExpoPush" of the appsettings.</summary>
    public sealed class ExpoPushSettings
    {
        public const string SECTION = "ExpoPush";

        public string BaseUrl { get; set; } = "https://exp.host";

        /// <summary>
        /// Optional. Only needed if "Enhanced Security for Push Notifications" is enabled in the Expo project.
        /// Never versioned: set it via user-secrets or the environment variable ExpoPush__AccessToken.
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>Android notification channel created by the app (src/lib/notifications.ts).</summary>
        public string ChannelId { get; set; } = "default";

        public int TimeoutSeconds { get; set; } = 15;
    }
}
