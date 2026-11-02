using Microsoft.AspNetCore.Http;

namespace MiCash.Exception.ExceptionBase.IA
{
    /// <summary>
    /// Unexpected failure talking to the AI provider.
    /// The message is always generic: provider details must be logged, never returned to the client.
    /// </summary>
    public sealed class AIUnknownException
    : AIServiceException
    {
        public AIUnknownException ()
            : base("Ocorreu um erro inesperado ao comunicar com o serviço de IA.")
        {
        }

        public override int StatusCode =>
            StatusCodes.Status500InternalServerError;
    }
}
