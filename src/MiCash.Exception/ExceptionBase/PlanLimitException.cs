using System.Net;

namespace MiCash.Exception.ExceptionBase
{
    /// <summary>
    /// The user's plan does not allow the action (Free limit reached or Premium-only feature).
    /// 403 with errorCode PLAN_LIMIT_REACHED: the app shows the subscription offer instead of an error.
    /// </summary>
    public class PlanLimitException : MiCashException
    {
        public const string CODE = "PLAN_LIMIT_REACHED";

        private readonly DateTime? _retryAt;

        public PlanLimitException ( string message , DateTime? retryAt = null ) : base(message)
        {
            _retryAt = retryAt;
        }

        public override int StatusCode => ( int ) HttpStatusCode.Forbidden;

        public override string? ErrorCode => CODE;

        public override DateTime? RetryAt => _retryAt;

        public override List<string> GetErrors () => [Message];
    }

    /// <summary>Premium chat usage above the daily or per-minute limit: 429 with the release time.</summary>
    public class ChatRateLimitException : MiCashException
    {
        public const string CODE = "CHAT_RATE_LIMIT";

        private readonly DateTime _retryAt;

        public ChatRateLimitException ( string message , DateTime retryAt ) : base(message)
        {
            _retryAt = retryAt;
        }

        public override int StatusCode => ( int ) HttpStatusCode.TooManyRequests;

        public override string? ErrorCode => CODE;

        public override DateTime? RetryAt => _retryAt;

        public override List<string> GetErrors () => [Message];
    }
}
