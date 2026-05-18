using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Users
{
    public interface IUserReadOnlyRepository
    {
        Task<bool> ExistActiveUserWithEmail(string email);
        Task<User?> GetByUserIdentifier ( Guid userIdentifier );
        Task<User?> GetUserByEmail(string email);
    }
}
