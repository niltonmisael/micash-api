using System.Text.Json.Serialization;

namespace MiCash.Communication.Responses
{
    public class ResponseErrorJson
    {
        public List<string> ErrorMessages { get; set; }

        /// <summary>Machine-readable code (PLAN_LIMIT_REACHED, CHAT_RATE_LIMIT); omitted for most errors.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorCode { get; set; }

        /// <summary>When the blocked action is allowed again (UTC); omitted when not applicable.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? RetryAt { get; set; }

        public ResponseErrorJson ( string errorMessage )
        {
            ErrorMessages = [errorMessage];
        }

        public ResponseErrorJson ( List<string> errorMessage )
        {
            ErrorMessages = errorMessage;
        }
    }
}
