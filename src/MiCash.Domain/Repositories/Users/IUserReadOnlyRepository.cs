using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.Users
{
    public interface IUserReadOnlyRepository
    {
        Task<bool> ExistActiveUserWithEmail(string email);
        Task<User?> GetByIdAsync ( long userId , CancellationToken cancellationToken );

        /// <summary>
        /// Ids (ascending, after <paramref name="afterId"/>) of verified users that want the monthly summary by
        /// e-mail or push and did not get the one of <paramref name="month"/> (first day) yet.
        /// </summary>
        Task<IReadOnlyList<long>> GetMonthlySummaryRecipientIdsAsync ( DateOnly month , long afterId , int take , CancellationToken cancellationToken );
        Task<User?> GetByUserIdentifier ( Guid userIdentifier );
        Task<User?> GetUserByEmail(string email);
    }
}
