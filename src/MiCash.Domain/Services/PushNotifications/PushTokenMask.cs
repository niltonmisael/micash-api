using System.Text.RegularExpressions;

namespace MiCash.Domain.Services.PushNotifications
{
    /// <summary>Expo push tokens identify a device: they never go to logs or API responses in full.</summary>
    public static partial class PushTokenMask
    {
        private const int VISIBLE_CHARS = 6;

        /// <summary>"ExponentPushToken[abcdefghij123456]" → "…123456".</summary>
        public static string Suffix ( string? token )
        {
            if ( string.IsNullOrEmpty(token) ) {
                return string.Empty;
            }

            var inner = token.TrimEnd(']');
            var start = inner.IndexOf('[');
            inner = start >= 0 ? inner[(start + 1)..] : inner;

            return inner.Length <= VISIBLE_CHARS ? "…" : "…" + inner[^VISIBLE_CHARS..];
        }

        /// <summary>Masks every token inside a free text (Expo error messages quote the token).</summary>
        public static string? Sanitize ( string? text )
        {
            if ( string.IsNullOrEmpty(text) ) {
                return text;
            }

            return TokenRegex().Replace(text , match => $"{match.Groups["prefix"].Value}[{Suffix(match.Value)}]");
        }

        [GeneratedRegex(@"(?<prefix>Expo(nent)?PushToken)\[[^\]]*\]")]
        private static partial Regex TokenRegex ();
    }
}
