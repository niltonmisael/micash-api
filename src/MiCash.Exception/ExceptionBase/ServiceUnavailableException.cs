using System.Net;

namespace MiCash.Exception.ExceptionBase
{
    /// <summary>
    /// A service the operation depends on (e.g. Pagar.me) failed for now; nothing was changed and the
    /// user can try again. 503 with the given message.
    /// </summary>
    public class ServiceUnavailableException : MiCashException
    {
        public ServiceUnavailableException ( string message ) : base(message)
        {
        }

        public override int StatusCode => ( int ) HttpStatusCode.ServiceUnavailable;

        public override List<string> GetErrors ()
        {
            return [Message];
        }
    }
}
