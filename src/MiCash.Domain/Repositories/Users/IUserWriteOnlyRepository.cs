namespace MiCash.Domain.Repositories.Users
{
    public interface IUserWriteOnlyRepository
    {
        Task Add(Entities.User user);
        Task Delete(Entities.User user);
    }
}
