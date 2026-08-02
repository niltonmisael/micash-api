using Microsoft.AspNetCore.Http;

namespace MiCash.Exception.ExceptionBase.IA
{
    public sealed class AIAuthenticationException : AIServiceException
    {
        public AIAuthenticationException ()
            : base("Falha na autenticação com o serviço de IA.")
        {
        }

        public override int StatusCode =>
            StatusCodes.Status401Unauthorized;
    }
}
