using System.Net;

namespace MiCash.Exception.ExceptionBase
{
    public class BadRequestException : MiCashException
    {

        private readonly List<string> _errors;

        public override int StatusCode => ( int ) HttpStatusCode.BadRequest;

        public BadRequestException ( List<string> errorMessages ) : base(string.Empty)
        {
            _errors = errorMessages;
        }

        public override List<string> GetErrors ()
        {
            return _errors;
        }
    }
}
