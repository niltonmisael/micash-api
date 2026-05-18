using System.Net;

namespace MiCash.Exception.ExceptionBase
{
    public class InvalidTokenException : MiCashException
    {
        public InvalidTokenException () : base(ResourceErrorMessages.INVALID_TOKEN) { }

        public override int StatusCode => ( int ) HttpStatusCode.Unauthorized;

        public override List<string> GetErrors ()
        {
            return [Message];
        }
    }
}
