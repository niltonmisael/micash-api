using Microsoft.AspNetCore.Http;

namespace MiCash.Exception.ExceptionBase.IA
{
    public sealed class AIRateLimitException
        : AIServiceException
    {
        public AIRateLimitException ()
            : base("O limite de requisições da IA foi excedido.")
        {
        }

        public override int StatusCode =>
            StatusCodes.Status429TooManyRequests;
    }
}
