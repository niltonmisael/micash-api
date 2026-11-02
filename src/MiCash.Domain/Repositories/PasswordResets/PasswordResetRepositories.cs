using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.PasswordResets
{
    public interface IPasswordResetReadOnlyRepository
    {
        /// <summary>Requests created by the user since <paramref name="since"/> (hourly limit).</summary>
        Task<int> CountCreatedSinceAsync ( long userId , DateTime since , CancellationToken cancellationToken );

        /// <summary>Creation time of the user's latest request (resend interval), or null.</summary>
        Task<DateTime?> GetLatestCreatedAtAsync ( long userId , CancellationToken cancellationToken );
    }

    public interface IPasswordResetWriteOnlyRepository
    {
        Task AddAsync ( PasswordResetRequest request , CancellationToken cancellationToken );
    }

    /// <summary>Tracked entities; saved by <see cref="IUnitOfWork.Commit"/> (optimistic concurrency).</summary>
    public interface IPasswordResetUpdateOnlyRepository
    {
        Task<PasswordResetRequest?> GetByIdAsync ( long id , CancellationToken cancellationToken );

        /// <summary>Requests not completed nor invalidated.</summary>
        Task<List<PasswordResetRequest>> GetOpenByUserAsync ( long userId , CancellationToken cancellationToken );

        /// <summary>The user's latest open request whose code was already sent and not yet verified.</summary>
        Task<PasswordResetRequest?> GetPendingVerificationAsync ( long userId , CancellationToken cancellationToken );

        Task<PasswordResetRequest?> GetByResetTokenHashAsync ( string resetTokenHash , CancellationToken cancellationToken );
    }
}
