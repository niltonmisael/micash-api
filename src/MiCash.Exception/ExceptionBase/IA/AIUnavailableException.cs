using Microsoft.AspNetCore.Http;

namespace MiCash.Exception.ExceptionBase.IA
{
    public sealed class AIUnavailableException
        : AIServiceException
    {
        public AIUnavailableException ()
            : base("O serviço de IA encontra-se indisponível.")
        {
        }

        public override int StatusCode =>
            StatusCodes.Status503ServiceUnavailable;
    }
}
