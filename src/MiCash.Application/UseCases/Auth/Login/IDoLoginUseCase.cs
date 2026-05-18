using MiCash.Communication.Requests;
using MiCash.Communication.Responses;

namespace MiCash.Application.UseCases.Auth.Login
{
    public interface IDoLoginUseCase
    {
        Task<ResponseRegisteredUserJson> Execute ( RequestLoginJson request );
    }
}
