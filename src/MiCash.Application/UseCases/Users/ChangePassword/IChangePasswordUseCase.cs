using MiCash.Communication.Requests;

namespace MiCash.Application.UseCases.Users.ChangePassword
{
    public interface IChangePasswordUseCase
    {
        Task Execute ( RequestChangePasswordJson request, CancellationToken cancellationToken );
    }
}
