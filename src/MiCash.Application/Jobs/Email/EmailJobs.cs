using MiCash.Application.UseCases.Auth.PasswordReset;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.PasswordResets;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Services.Email;
using MiCash.Domain.Services.Jobs;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging;

namespace MiCash.Application.Jobs.Email
{
    /// <summary>
    /// Welcome e-mail after registration. Runs in Hangfire: the account already exists, a provider failure
    /// only delays the e-mail. Temporary failures are rethrown (Hangfire retries); the idempotency key keeps
    /// a retry from sending it twice.
    /// </summary>
    public sealed class WelcomeEmailJob : IWelcomeEmailJob
    {
        private readonly IUserReadOnlyRepository _users;
        private readonly ITransactionalEmailService _emails;
        private readonly ILogger<WelcomeEmailJob> _logger;

        public WelcomeEmailJob ( IUserReadOnlyRepository users , ITransactionalEmailService emails , ILogger<WelcomeEmailJob> logger )
        {
            _users = users;
            _emails = emails;
            _logger = logger;
        }

        public async Task ExecuteAsync ( long userId , CancellationToken cancellationToken )
        {
            var user = await _users.GetByIdAsync(userId , cancellationToken);

            if ( user is null ) {
                _logger.LogInformation("Welcome e-mail skipped: user {UserId} no longer exists." , userId);
                return;
            }

            await EmailJobSender.SendAsync(
                () => _emails.SendAsync(user.Email , new WelcomeEmail(user.Name) , $"welcome-{user.Id}" , cancellationToken) ,
                _logger , "welcome" , userId);
        }
    }

    /// <summary>
    /// Generates the code of a password reset request, stores only its hash and e-mails it.
    /// Each run makes a new code (the plain code exists only in memory), so a retry never sends a code that
    /// no longer matches the stored hash. Requests already replaced, used or expired are skipped.
    /// </summary>
    public sealed class PasswordResetEmailJob : IPasswordResetEmailJob
    {
        private readonly IPasswordResetUpdateOnlyRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordEncripter _encripter;
        private readonly ITransactionalEmailService _emails;
        private readonly TimeProvider _time;
        private readonly ILogger<PasswordResetEmailJob> _logger;

        public PasswordResetEmailJob (
            IPasswordResetUpdateOnlyRepository repository ,
            IUnitOfWork unitOfWork ,
            IPasswordEncripter encripter ,
            ITransactionalEmailService emails ,
            TimeProvider time ,
            ILogger<PasswordResetEmailJob> logger )
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _encripter = encripter;
            _emails = emails;
            _time = time;
            _logger = logger;
        }

        public async Task ExecuteAsync ( long passwordResetRequestId , CancellationToken cancellationToken )
        {
            var now = _time.GetUtcNow().UtcDateTime;
            var reset = await _repository.GetByIdAsync(passwordResetRequestId , cancellationToken);

            // Retries end here once the request is no longer usable (replaced, verified, expired).
            if ( reset is null || reset.IsOpen == false || reset.VerifiedAt is not null
                || reset.CreatedAt + PasswordResetRules.CodeValidity <= now ) {
                _logger.LogInformation("Password reset e-mail skipped: request {RequestId} is no longer pending." , passwordResetRequestId);
                return;
            }

            var code = PasswordResetRules.NewCode();

            reset.CodeHash = _encripter.Encrypt(code);
            reset.CodeSentAt = now;
            reset.ExpiresAt = now + PasswordResetRules.CodeValidity;
            reset.Attempts = 0;

            try {
                await _unitOfWork.Commit(cancellationToken);
            } catch ( ConflictException ) {
                // Replaced by a newer request meanwhile: that one sends its own code.
                _logger.LogInformation("Password reset e-mail skipped: request {RequestId} changed meanwhile." , passwordResetRequestId);
                return;
            }

            var email = new PasswordRecoveryEmail(
                reset.User.Name ,
                code ,
                (int)PasswordResetRules.CodeValidity.TotalMinutes);

            await EmailJobSender.SendAsync(
                () => _emails.SendAsync(reset.User.Email , email , idempotencyKey: null , cancellationToken) ,
                _logger , "password reset" , reset.UserId);
        }
    }

    internal static class EmailJobSender
    {
        /// <summary>
        /// Temporary provider failure → rethrow (Hangfire retries). Permanent failure (invalid address, refused)
        /// → logged, not retried. Logs carry only ids, never addresses, codes or tokens.
        /// </summary>
        public static async Task SendAsync ( Func<Task<string>> send , ILogger logger , string kind , long userId )
        {
            try {
                await send();
            } catch ( EmailServiceException ex ) when ( ex.IsTransient == false ) {
                logger.LogWarning("The {Kind} e-mail of user {UserId} was refused ({ErrorCode}) and will not be retried." , kind , userId , ex.ErrorCode);
            } catch ( ArgumentException ) {
                logger.LogWarning("The {Kind} e-mail of user {UserId} was not sent: invalid recipient or content." , kind , userId);
            }
        }
    }
}
