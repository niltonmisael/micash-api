using System.Net;

namespace MiCash.Exception.ExceptionBase
{
    public class NotFoundException : MiCashException
    {
        public NotFoundException (string message) : base(message)
        {
        }

        public override int StatusCode => (int)HttpStatusCode.NotFound;

        public override List<string> GetErrors ()
        {
            return [Message];
        }
    }
}
