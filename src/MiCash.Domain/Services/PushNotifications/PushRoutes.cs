using System.Text.RegularExpressions;

namespace MiCash.Domain.Services.PushNotifications
{
    /// <summary>
    /// Routes of the mobile app (Expo Router) a notification may open via data.url.
    /// Only these paths, optionally with a simple query string (e.g. "/savings-goal?id=5"),
    /// are accepted: never external URLs.
    /// </summary>
    public static partial class PushRoutes
    {
        public static readonly IReadOnlySet<string> AllowedPaths = new HashSet<string>(StringComparer.Ordinal)
        {
            "/home",
            "/transactions",
            "/reports",
            "/profile",
            "/categories",
            "/goals",
            "/savings-goal",
            "/new-transaction",
            "/assistant",
            "/assistant-tips",
            "/notifications",
            "/subscription"
        };

        public static bool IsAllowed ( string? route )
        {
            if ( string.IsNullOrEmpty(route) ) {
                return false;
            }

            var match = RouteRegex().Match(route);

            return match.Success && AllowedPaths.Contains(match.Groups["path"].Value);
        }

        [GeneratedRegex(@"^(?<path>/[a-z-]+)(\?[A-Za-z0-9_\-]+=[A-Za-z0-9_\-]*(&[A-Za-z0-9_\-]+=[A-Za-z0-9_\-]*)*)?$")]
        private static partial Regex RouteRegex ();
    }
}
