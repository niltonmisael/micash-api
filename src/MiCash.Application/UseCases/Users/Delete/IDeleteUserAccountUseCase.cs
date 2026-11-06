using MiCash.Communication.Requests.Users;

namespace MiCash.Application.UseCases.Users.Delete
{
    public interface IDeleteUserAccountUseCase
    {
        Task Execute ( RequestDeleteAccountJson request , CancellationToken cancellationToken );
    }
}
