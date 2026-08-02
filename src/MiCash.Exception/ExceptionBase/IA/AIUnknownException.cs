using Microsoft.AspNetCore.Http;

namespace MiCash.Exception.ExceptionBase.IA
{
    public sealed class AIUnknownException
    : AIServiceException
    {
        public AIUnknownException ( string message )
            : base(message)
        {
        }

        public override int StatusCode =>
            StatusCodes.Status500InternalServerError;
    }
}
