using Microsoft.AspNetCore.Http;

namespace MiCash.Exception.ExceptionBase.IA
{
    public sealed class AIInvalidResponseException
        : AIServiceException
    {
        public AIInvalidResponseException ()
            : base("A IA retornou uma resposta inválida.")
        {
        }

        public override int StatusCode =>
            StatusCodes.Status502BadGateway;
    }
}
