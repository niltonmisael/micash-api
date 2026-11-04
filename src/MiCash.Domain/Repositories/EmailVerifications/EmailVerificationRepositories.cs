using MiCash.Domain.Entities;
using MiCash.Domain.Enums;

namespace MiCash.Domain.Repositories.EmailVerifications
{
    public interface IEmailVerificationReadOnlyRepository
    {
        /// <summary>Requests created by the user since <paramref name="since"/> (hourly limit).</summary>
        Task<int> CountCreatedSinceAsync ( long userId , DateTime since , CancellationToken cancellationToken );

        /// <summary>Creation time of the user's latest request of <paramref name="purpose"/> (resend interval), or null.</summary>
        Task<DateTime?> GetLatestCreatedAtAsync ( long userId , EmailVerificationPurpose purpose , CancellationToken cancellationToken );
    }

    public interface IEmailVerificationWriteOnlyRepository
    {
        Task AddAsync ( EmailVerificationRequest request , CancellationToken cancellationToken );
    }

    /// <summary>Tracked entities; saved by <see cref="IUnitOfWork.Commit"/> (optimistic concurrency).</summary>
    public interface IEmailVerificationUpdateOnlyRepository
    {
        Task<EmailVerificationRequest?> GetByIdAsync ( long id , CancellationToken cancellationToken );

        /// <summary>Requests not verified nor invalidated.</summary>
        Task<List<EmailVerificationRequest>> GetOpenByUserAsync ( long userId , CancellationToken cancellationToken );

        /// <summary>The user's latest open request of <paramref name="purpose"/> whose code was already sent.</summary>
        Task<EmailVerificationRequest?> GetPendingVerificationAsync ( long userId , EmailVerificationPurpose purpose , CancellationToken cancellationToken );
    }
}
