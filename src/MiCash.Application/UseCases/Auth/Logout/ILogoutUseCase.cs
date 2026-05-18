using MiCash.Communication.Requests;

namespace MiCash.Application.UseCases.Login.Logout
{
    public interface ILogoutUseCase
    {
        Task Execute ( RequestLogoutJson request );
    }
}
