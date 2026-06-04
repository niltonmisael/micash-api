namespace MiCash.Application.UseCases.Users.Delete
{
    public interface IDeleteUserAccountUseCase
    {
        Task Execute (CancellationToken cancellationToken);
    }
}
