using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.EmailVerifications;
using MiCash.Domain.Security.Cryptography;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging;

namespace MiCash.Application.UseCases.Auth.EmailVerification
{
    /// <summary>
    /// Checks a typed code against the user's latest pending request of a purpose (sign-up or e-mail change).
    /// Every failure (no code, wrong, expired, too many attempts, stale address) is the same 400 message.
    /// The attempt is counted and saved before comparing, so parallel guesses conflict on the row version.
    /// </summary>
    public sealed class EmailVerificationCodeChecker
    {
        // BCrypt hash compared when there is nothing to compare, so a missing code takes as long as a wrong one.
        private static string? _dummyHash;

        private readonly IEmailVerificationUpdateOnlyRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordEncripter _encripter;
        private readonly ILogger<EmailVerificationCodeChecker> _logger;

        public EmailVerificationCodeChecker (
            IEmailVerificationUpdateOnlyRepository repository ,
            IUnitOfWork unitOfWork ,
            IPasswordEncripter encripter ,
            ILogger<EmailVerificationCodeChecker> logger )
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _encripter = encripter;
            _logger = logger;
        }

        /// <summary>The matching request (still open: the caller sets VerifiedAt and commits), or 400.</summary>
        public async Task<EmailVerificationRequest> CheckAsync (
            User user , EmailVerificationPurpose purpose , string code , DateTime now , CancellationToken cancellationToken )
        {
            var verification = await _repository.GetPendingVerificationAsync(user.Id , purpose , cancellationToken);

            if ( verification is null
                || StillApplies(verification , user) == false
                || verification.ExpiresAt <= now
                || verification.Attempts >= EmailVerificationRules.MAX_ATTEMPTS ) {
                _encripter.Verify(code , _dummyHash ??= _encripter.Encrypt(Guid.NewGuid().ToString()));

                if ( verification is not null ) {
                    verification.InvalidatedAt = now;
                    await CommitOrFailAsync(cancellationToken);
                }

                throw InvalidCode();
            }

            verification.Attempts++;
            await CommitOrFailAsync(cancellationToken);

            if ( _encripter.Verify(code , verification.CodeHash!) == false ) {
                if ( verification.Attempts >= EmailVerificationRules.MAX_ATTEMPTS ) {
                    verification.InvalidatedAt = now;
                    await CommitOrFailAsync(cancellationToken);
                    _logger.LogWarning("E-mail verification request {RequestId} invalidated: too many wrong codes." , verification.Id);
                }

                throw InvalidCode();
            }

            return verification;
        }

        /// <summary>
        /// SignUp: the code must have gone to the current account e-mail (a corrected address voids it).
        /// EmailChange: the account must not have that address already (change applied meanwhile).
        /// </summary>
        public static bool StillApplies ( EmailVerificationRequest verification , User user ) =>
            verification.Purpose == EmailVerificationPurpose.SignUp
                ? verification.Email == user.Email
                : verification.Email != user.Email;

        /// <summary>Commit whose concurrency conflict means "someone else used this code": same 400 message.</summary>
        public async Task CommitOrFailAsync ( CancellationToken cancellationToken )
        {
            try {
                await _unitOfWork.Commit(cancellationToken);
            } catch ( ConflictException ) {
                throw InvalidCode();
            }
        }

        public static ErrorOnValidationException InvalidCode () =>
            new([ResourceErrorMessages.EMAIL_VERIFICATION_CODE_INVALID]);
    }
}
