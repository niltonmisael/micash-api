using Microsoft.AspNetCore.Http;

namespace MiCash.Exception.ExceptionBase.IA
{
    /// <summary>
    /// The API key/project was rejected by the AI provider. This is a server misconfiguration,
    /// never the user's fault: it must NOT be 401, otherwise the app treats it as an expired
    /// session and logs the user out.
    /// </summary>
    public sealed class AIAuthenticationException : AIServiceException
    {
        public AIAuthenticationException ()
            : base("O serviço de IA encontra-se indisponível.")
        {
        }

        public override int StatusCode =>
            StatusCodes.Status503ServiceUnavailable;
    }
}
