using System.Net;

namespace MiCash.Exception.ExceptionBase
{
    /// <summary>
    /// The operation collided with a concurrent one (e.g. unique constraint violated by a double tap).
    /// </summary>
    public class ConflictException : MiCashException
    {
        public ConflictException () : base(ResourceErrorMessages.RESOURCE_CONFLICT)
        {
        }

        public override int StatusCode => ( int ) HttpStatusCode.Conflict;

        public override List<string> GetErrors ()
        {
            return [Message];
        }
    }
}
