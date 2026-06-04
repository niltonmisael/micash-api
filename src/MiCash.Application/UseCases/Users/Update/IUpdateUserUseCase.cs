using MiCash.Communication.Requests;

namespace MiCash.Application.UseCases.Users.Update
{
    public interface IUpdateUserUseCase
    {
        Task Execute (RequestUpdateUserJson request, CancellationToken cancellationToken );
    }
}
