using System.Net;

namespace MiCash.Exception.ExceptionBase
{
    /// <summary>429 with the time the action is allowed again (e.g. resend of the e-mail verification code).</summary>
    public class TooManyRequestsException : MiCashException
    {
        private readonly DateTime _retryAt;

        public TooManyRequestsException ( string message , DateTime retryAt ) : base(message)
        {
            _retryAt = retryAt;
        }

        public override int StatusCode => ( int ) HttpStatusCode.TooManyRequests;

        public override DateTime? RetryAt => _retryAt;

        public override List<string> GetErrors () => [Message];
    }
}
