using MiCash.Communication.Requests;
using MiCash.Communication.Responses;

namespace MiCash.Application.UseCases.Login.Refresh
{
    public interface IRefreshTokenUseCase
    {
        Task<ResponseRegisteredUserJson> Execute ( RequestRefreshTokenJson request );
    }
}
