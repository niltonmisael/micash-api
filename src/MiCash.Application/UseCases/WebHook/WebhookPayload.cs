using System.Globalization;
using System.Text.Json;

namespace MiCash.Application.UseCases.WebHook
{
    /// <summary>Null-tolerant readers for Pagar.me webhook payloads.</summary>
    internal static class WebhookPayload
    {
        /// <summary>Follows <paramref name="path"/>; null when a segment is missing or JSON null.</summary>
        public static JsonElement? Get ( JsonElement element , params string[] path )
        {
            var current = element;

            foreach ( var segment in path ) {
                if ( current.ValueKind != JsonValueKind.Object ||
                     !current.TryGetProperty(segment , out current) ||
                     current.ValueKind == JsonValueKind.Null ) {
                    return null;
                }
            }

            return current;
        }

        public static string? GetString ( JsonElement element , params string[] path )
        {
            var value = Get(element , path);

            return value?.ValueKind == JsonValueKind.String ? value.Value.GetString() : null;
        }

        /// <summary>
        /// ISO-8601 date as UTC. Offsets ("Z", "-03:00") are honored; a value without offset is taken as UTC.
        /// Never depends on the server's time zone.
        /// </summary>
        public static DateTime? GetUtcDateTime ( JsonElement element , params string[] path )
        {
            var raw = GetString(element , path);

            if ( string.IsNullOrWhiteSpace(raw) ) {
                return null;
            }

            return DateTimeOffset.TryParse(
                    raw ,
                    CultureInfo.InvariantCulture ,
                    DateTimeStyles.AssumeUniversal ,
                    out var parsed)
                ? parsed.UtcDateTime
                : null;
        }
    }
}
