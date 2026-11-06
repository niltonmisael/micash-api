using MiCash.Application.UseCases.Auth.EmailVerification;
using MiCash.Application.UseCases.Auth.PasswordReset;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.EmailVerifications;
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

    /// <summary>
    /// Generates the code of an e-mail verification request, stores only its hash and e-mails it (template 01).
    /// Same retry rules as the password reset: each run makes a new code; requests replaced, used, expired, of an
    /// already verified account or of an address the user corrected meanwhile are skipped.
    /// </summary>
    public sealed class EmailVerificationEmailJob : IEmailVerificationEmailJob
    {
        private readonly IEmailVerificationUpdateOnlyRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordEncripter _encripter;
        private readonly ITransactionalEmailService _emails;
        private readonly TimeProvider _time;
        private readonly ILogger<EmailVerificationEmailJob> _logger;

        public EmailVerificationEmailJob (
            IEmailVerificationUpdateOnlyRepository repository ,
            IUnitOfWork unitOfWork ,
            IPasswordEncripter encripter ,
            ITransactionalEmailService emails ,
            TimeProvider time ,
            ILogger<EmailVerificationEmailJob> logger )
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _encripter = encripter;
            _emails = emails;
            _time = time;
            _logger = logger;
        }

        public async Task ExecuteAsync ( long emailVerificationRequestId , CancellationToken cancellationToken )
        {
            var now = _time.GetUtcNow().UtcDateTime;
            var verification = await _repository.GetByIdAsync(emailVerificationRequestId , cancellationToken);

            // Sign-up: account still unverified at the same address. E-mail change: not applied yet.
            if ( verification is null || verification.IsOpen == false
                || (verification.Purpose == EmailVerificationPurpose.SignUp && verification.User.EmailVerifiedAt is not null)
                || EmailVerificationCodeChecker.StillApplies(verification , verification.User) == false
                || verification.CreatedAt + EmailVerificationRules.CodeValidity <= now ) {
                _logger.LogInformation("E-mail verification code skipped: request {RequestId} is no longer pending." , emailVerificationRequestId);
                return;
            }

            var code = EmailVerificationRules.NewCode();

            verification.CodeHash = _encripter.Encrypt(code);
            verification.CodeSentAt = now;
            verification.ExpiresAt = now + EmailVerificationRules.CodeValidity;
            verification.Attempts = 0;

            try {
                await _unitOfWork.Commit(cancellationToken);
            } catch ( ConflictException ) {
                _logger.LogInformation("E-mail verification code skipped: request {RequestId} changed meanwhile." , emailVerificationRequestId);
                return;
            }

            var minutes = (int)EmailVerificationRules.CodeValidity.TotalMinutes;

            TransactionalEmail email = verification.Purpose == EmailVerificationPurpose.EmailChange
                ? new EmailChangeConfirmationEmail(verification.User.Name , code , minutes)
                : new EmailVerificationEmail(verification.User.Name , code , minutes);

            await EmailJobSender.SendAsync(
                () => _emails.SendAsync(verification.Email , email , idempotencyKey: null , cancellationToken) ,
                _logger , "e-mail verification" , verification.UserId);
        }
    }

    /// <summary>
    /// Security notice to the PREVIOUS address after a confirmed e-mail change (template 07): if the owner did not
    /// ask for it, they know right away and can contact support. The idempotency key keeps a retry from sending
    /// it twice.
    /// </summary>
    public sealed class EmailChangedNoticeJob : IEmailChangedNoticeJob
    {
        private readonly IEmailVerificationUpdateOnlyRepository _repository;
        private readonly ITransactionalEmailService _emails;
        private readonly ILogger<EmailChangedNoticeJob> _logger;

        public EmailChangedNoticeJob (
            IEmailVerificationUpdateOnlyRepository repository ,
            ITransactionalEmailService emails ,
            ILogger<EmailChangedNoticeJob> logger )
        {
            _repository = repository;
            _emails = emails;
            _logger = logger;
        }

        public async Task ExecuteAsync ( long emailVerificationRequestId , string previousEmail , CancellationToken cancellationToken )
        {
            var verification = await _repository.GetByIdAsync(emailVerificationRequestId , cancellationToken);

            if ( verification is null || verification.Purpose != EmailVerificationPurpose.EmailChange || verification.VerifiedAt is null ) {
                _logger.LogInformation("E-mail changed notice skipped: request {RequestId} is not a confirmed change." , emailVerificationRequestId);
                return;
            }

            await EmailJobSender.SendAsync(
                () => _emails.SendAsync(
                    previousEmail ,
                    new EmailChangedNoticeEmail(verification.User.Name , verification.Email) ,
                    $"email-changed-{verification.Id}" ,
                    cancellationToken) ,
                _logger , "e-mail changed notice" , verification.UserId);
        }
    }

    /// <summary>
    /// Notice to the account address after the account was deleted (template 08): if the owner did not do it, they
    /// know right away. The idempotency key keeps a retry from sending it twice.
    /// </summary>
    public sealed class AccountDeletedEmailJob : IAccountDeletedEmailJob
    {
        private readonly ITransactionalEmailService _emails;
        private readonly ILogger<AccountDeletedEmailJob> _logger;

        public AccountDeletedEmailJob ( ITransactionalEmailService emails , ILogger<AccountDeletedEmailJob> logger )
        {
            _emails = emails;
            _logger = logger;
        }

        public async Task ExecuteAsync ( long userId , string email , string name , CancellationToken cancellationToken )
        {
            await EmailJobSender.SendAsync(
                () => _emails.SendAsync(email , new AccountDeletedEmail(name) , $"account-deleted-{userId}" , cancellationToken) ,
                _logger , "account deleted" , userId);
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
