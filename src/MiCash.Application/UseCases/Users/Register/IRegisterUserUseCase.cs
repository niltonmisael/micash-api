using MiCash.Communication.Requests;
using MiCash.Communication.Responses;

namespace MiCash.Application.UseCases.Users.Register
{
    public interface IRegisterUserUseCase
    {
        Task<ResponseRegisteredUserJson> Execute (RequestRegisterUserJson request, CancellationToken cancellationToken );
    }
}
