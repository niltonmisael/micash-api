using MiCash.Application.UseCases.Auth.EmailVerification;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Services.Jobs;
using Microsoft.Extensions.Logging;

namespace MiCash.Application.Jobs.Users
{
    /// <summary>
    /// Daily: deletes accounts that never confirmed the e-mail within
    /// <see cref="EmailVerificationRules.UnverifiedAccountLifetime"/>, which frees the address for its real owner.
    /// Unverified accounts cannot use the API besides the verification endpoints, so there is nothing of theirs
    /// (subscriptions, data) to keep. Deletes in batches so a single run never loads too much.
    /// </summary>
    public sealed class UnverifiedAccountCleanupJob : IUnverifiedAccountCleanupJob
    {
        public const int BATCH_SIZE = 200;

        /// <summary>Upper bound per run; whatever is left goes in the next day's run.</summary>
        public const int MAX_BATCHES = 50;

        private readonly IUserWriteOnlyRepository _users;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _time;
        private readonly ILogger<UnverifiedAccountCleanupJob> _logger;

        public UnverifiedAccountCleanupJob (
            IUserWriteOnlyRepository users ,
            IUnitOfWork unitOfWork ,
            TimeProvider time ,
            ILogger<UnverifiedAccountCleanupJob> logger )
        {
            _users = users;
            _unitOfWork = unitOfWork;
            _time = time;
            _logger = logger;
        }

        public async Task ExecuteAsync ( CancellationToken cancellationToken )
        {
            var cutoff = _time.GetUtcNow().UtcDateTime - EmailVerificationRules.UnverifiedAccountLifetime;
            var total = 0;

            for ( var batch = 0; batch < MAX_BATCHES; batch++ ) {
                var removed = await _users.RemoveUnverifiedCreatedBeforeAsync(cutoff , BATCH_SIZE , cancellationToken);

                if ( removed == 0 ) {
                    break;
                }

                await _unitOfWork.Commit(cancellationToken);
                total += removed;

                if ( removed < BATCH_SIZE ) {
                    break;
                }
            }

            if ( total > 0 ) {
                _logger.LogInformation("Deleted {Count} account(s) never verified since before {Cutoff:o}." , total , cutoff);
            }
        }
    }
}
