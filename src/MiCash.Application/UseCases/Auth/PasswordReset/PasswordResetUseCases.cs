using FluentValidation;
using MiCash.Communication.Requests.PasswordReset;
using MiCash.Communication.Responses.PasswordReset;
using MiCash.Domain.Entities;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.PasswordResets;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Models;
using MiCash.Domain.Services.Jobs;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging;

namespace MiCash.Application.UseCases.Auth.PasswordReset
{
    public interface IRequestPasswordResetUseCase
    {
        Task<ResponseForgotPasswordJson> Execute ( RequestForgotPasswordJson request , CancellationToken cancellationToken );
    }

    public interface IVerifyPasswordResetCodeUseCase
    {
        Task<ResponsePasswordResetCodeVerifiedJson> Execute ( RequestVerifyPasswordResetCodeJson request , CancellationToken cancellationToken );
    }

    public interface IResetPasswordUseCase
    {
        Task Execute ( RequestResetPasswordJson request , CancellationToken cancellationToken );
    }

    /// <summary>
    /// POST /forgot-password. Always the same answer (account or not, limit reached or not), and the e-mail is
    /// sent by a Hangfire job, so neither the content nor the response time reveals whether the e-mail is registered.
    /// </summary>
    public sealed class RequestPasswordResetUseCase : IRequestPasswordResetUseCase
    {
        private readonly IUserReadOnlyRepository _users;
        private readonly IPasswordResetReadOnlyRepository _readRepository;
        private readonly IPasswordResetWriteOnlyRepository _writeRepository;
        private readonly IPasswordResetUpdateOnlyRepository _updateRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBackgroundJobService _backgroundJobs;
        private readonly TimeProvider _time;
        private readonly ILogger<RequestPasswordResetUseCase> _logger;

        public RequestPasswordResetUseCase (
            IUserReadOnlyRepository users ,
            IPasswordResetReadOnlyRepository readRepository ,
            IPasswordResetWriteOnlyRepository writeRepository ,
            IPasswordResetUpdateOnlyRepository updateRepository ,
            IUnitOfWork unitOfWork ,
            IBackgroundJobService backgroundJobs ,
            TimeProvider time ,
            ILogger<RequestPasswordResetUseCase> logger )
        {
            _users = users;
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _updateRepository = updateRepository;
            _unitOfWork = unitOfWork;
            _backgroundJobs = backgroundJobs;
            _time = time;
            _logger = logger;
        }

        public static ResponseForgotPasswordJson Response () => new()
        {
            ResendAvailableInSeconds = (int)PasswordResetRules.ResendInterval.TotalSeconds ,
            CodeExpiresInMinutes = (int)PasswordResetRules.CodeValidity.TotalMinutes ,
            CodeLength = PasswordResetRules.CODE_LENGTH
        };

        public async Task<ResponseForgotPasswordJson> Execute ( RequestForgotPasswordJson request , CancellationToken cancellationToken )
        {
            PasswordResetValidation.ThrowIfInvalid(new ForgotPasswordValidator().Validate(request));

            // Same normalization as login/registration: the e-mail is compared as typed, without spaces around.
            var user = await _users.GetUserByEmail(request.Email.Trim());

            if ( user is not null ) {
                await StartAsync(user , cancellationToken);
            }

            return Response();
        }

        private async Task StartAsync ( User user , CancellationToken cancellationToken )
        {
            var now = _time.GetUtcNow().UtcDateTime;

            var latest = await _readRepository.GetLatestCreatedAtAsync(user.Id , cancellationToken);

            if ( latest is not null && now - latest.Value < PasswordResetRules.ResendInterval ) {
                _logger.LogInformation("Password reset ignored for user {UserId}: resend interval not elapsed." , user.Id);
                return;
            }

            var lastHour = await _readRepository.CountCreatedSinceAsync(user.Id , now.AddHours(-1) , cancellationToken);

            if ( lastHour >= PasswordResetRules.MAX_REQUESTS_PER_HOUR ) {
                _logger.LogWarning("Password reset ignored for user {UserId}: hourly limit reached." , user.Id);
                return;
            }

            // A new code replaces the previous ones (and any reset token not used yet).
            foreach ( var open in await _updateRepository.GetOpenByUserAsync(user.Id , cancellationToken) ) {
                open.InvalidatedAt = now;
            }

            var resetRequest = new PasswordResetRequest
            {
                UserId = user.Id ,
                CreatedAt = now ,
                ExpiresAt = now + PasswordResetRules.CodeValidity
            };

            await _writeRepository.AddAsync(resetRequest , cancellationToken);

            try {
                await _unitOfWork.Commit(cancellationToken);
            } catch ( ConflictException ) {
                // A parallel request for the same user changed the open requests: that one sends the code.
                _logger.LogInformation("Password reset for user {UserId} superseded by a parallel request." , user.Id);
                return;
            }

            try {
                _backgroundJobs.EnqueuePasswordResetEmail(resetRequest.Id);
            } catch ( System.Exception ex ) {
                // The user can ask again after the resend interval; the answer stays the same.
                _logger.LogError(ex , "Could not enqueue the password reset e-mail of request {RequestId}." , resetRequest.Id);
            }
        }
    }

    /// <summary>
    /// POST /verify-password-reset-code. Every failure (unknown e-mail, no code, wrong, expired, used, too many
    /// attempts) has the same message. Success returns a single-use reset token, never a session.
    /// </summary>
    public sealed class VerifyPasswordResetCodeUseCase : IVerifyPasswordResetCodeUseCase
    {
        // BCrypt hash compared when there is nothing to compare, so a missing account/code takes as long as a wrong code.
        private static string? _dummyHash;

        private readonly IUserReadOnlyRepository _users;
        private readonly IPasswordResetUpdateOnlyRepository _updateRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordEncripter _encripter;
        private readonly TimeProvider _time;
        private readonly ILogger<VerifyPasswordResetCodeUseCase> _logger;

        public VerifyPasswordResetCodeUseCase (
            IUserReadOnlyRepository users ,
            IPasswordResetUpdateOnlyRepository updateRepository ,
            IUnitOfWork unitOfWork ,
            IPasswordEncripter encripter ,
            TimeProvider time ,
            ILogger<VerifyPasswordResetCodeUseCase> logger )
        {
            _users = users;
            _updateRepository = updateRepository;
            _unitOfWork = unitOfWork;
            _encripter = encripter;
            _time = time;
            _logger = logger;
        }

        public async Task<ResponsePasswordResetCodeVerifiedJson> Execute ( RequestVerifyPasswordResetCodeJson request , CancellationToken cancellationToken )
        {
            PasswordResetValidation.ThrowIfInvalid(new VerifyPasswordResetCodeValidator().Validate(request));

            var now = _time.GetUtcNow().UtcDateTime;

            var user = await _users.GetUserByEmail(request.Email.Trim());

            var reset = user is null
                ? null
                : await _updateRepository.GetPendingVerificationAsync(user.Id , cancellationToken);

            if ( reset is null || reset.ExpiresAt <= now || reset.Attempts >= PasswordResetRules.MAX_ATTEMPTS ) {
                _encripter.Verify(request.Code , _dummyHash ??= _encripter.Encrypt(Guid.NewGuid().ToString()));

                if ( reset is not null ) {
                    reset.InvalidatedAt = now;
                    await CommitOrFail(cancellationToken);
                }

                throw PasswordResetValidation.InvalidCode();
            }

            // Counted (and saved) before checking: parallel guesses conflict on the row version instead of
            // all reading the same count.
            reset.Attempts++;
            await CommitOrFail(cancellationToken);

            if ( _encripter.Verify(request.Code , reset.CodeHash!) == false ) {
                if ( reset.Attempts >= PasswordResetRules.MAX_ATTEMPTS ) {
                    reset.InvalidatedAt = now;
                    await CommitOrFail(cancellationToken);
                    _logger.LogWarning("Password reset request {RequestId} invalidated: too many wrong codes." , reset.Id);
                }

                throw PasswordResetValidation.InvalidCode();
            }

            var token = PasswordResetRules.NewResetToken();

            reset.VerifiedAt = now;
            reset.ResetTokenHash = PasswordResetRules.HashResetToken(token);
            reset.ResetTokenExpiresAt = now + PasswordResetRules.ResetTokenValidity;

            await CommitOrFail(cancellationToken);

            _logger.LogInformation("Password reset code verified for request {RequestId}." , reset.Id);

            return new ResponsePasswordResetCodeVerifiedJson
            {
                ResetToken = token ,
                ExpiresInMinutes = (int)PasswordResetRules.ResetTokenValidity.TotalMinutes
            };
        }

        private async Task CommitOrFail ( CancellationToken cancellationToken )
        {
            try {
                await _unitOfWork.Commit(cancellationToken);
            } catch ( ConflictException ) {
                throw PasswordResetValidation.InvalidCode();
            }
        }
    }

    /// <summary>
    /// POST /reset-password. Consumes the reset token (single use), sets the new password with the existing
    /// hash (BCrypt), closes every open request of the user and blocks the renewal of older sessions.
    /// The user is not signed in: they log in with the new password.
    /// </summary>
    public sealed class ResetPasswordUseCase : IResetPasswordUseCase
    {
        private readonly IPasswordResetUpdateOnlyRepository _updateRepository;
        private readonly IUserUpdateOnlyRepository _userUpdateRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordEncripter _encripter;
        private readonly IRedisCacheService _cache;
        private readonly TimeProvider _time;
        private readonly ILogger<ResetPasswordUseCase> _logger;

        public ResetPasswordUseCase (
            IPasswordResetUpdateOnlyRepository updateRepository ,
            IUserUpdateOnlyRepository userUpdateRepository ,
            IUnitOfWork unitOfWork ,
            IPasswordEncripter encripter ,
            IRedisCacheService cache ,
            TimeProvider time ,
            ILogger<ResetPasswordUseCase> logger )
        {
            _updateRepository = updateRepository;
            _userUpdateRepository = userUpdateRepository;
            _unitOfWork = unitOfWork;
            _encripter = encripter;
            _cache = cache;
            _time = time;
            _logger = logger;
        }

        public async Task Execute ( RequestResetPasswordJson request , CancellationToken cancellationToken )
        {
            PasswordResetValidation.ThrowIfInvalid(new ResetPasswordValidator().Validate(request));

            var now = _time.GetUtcNow().UtcDateTime;

            var reset = await _updateRepository.GetByResetTokenHashAsync(
                PasswordResetRules.HashResetToken(request.ResetToken.Trim()) , cancellationToken);

            if ( reset is null || reset.IsOpen == false || reset.VerifiedAt is null || reset.ResetTokenExpiresAt <= now ) {
                throw InvalidToken();
            }

            var user = await _userUpdateRepository.GetById(reset.UserId);

            // Same rule as change-password. The token is not consumed: the user can choose another password.
            if ( _encripter.Verify(request.NewPassword , user.PasswordHash) ) {
                throw new ErrorOnValidationException([ResourceErrorMessages.NEW_PASSWORD_SAME_AS_CURRENT]);
            }

            user.PasswordHash = _encripter.Encrypt(request.NewPassword);
            user.UpdatedAt = now;

            // The code reached this mailbox: that proves the address as well as /verify-email does. It also lets
            // the real owner take back an address someone registered and never confirmed.
            user.EmailVerifiedAt ??= now;
            _userUpdateRepository.Update(user);

            reset.CompletedAt = now;

            foreach ( var open in await _updateRepository.GetOpenByUserAsync(reset.UserId , cancellationToken) ) {
                if ( open.Id != reset.Id ) {
                    open.InvalidatedAt = now;
                }
            }

            try {
                await _unitOfWork.Commit(cancellationToken);
            } catch ( ConflictException ) {
                // The same token used twice at the same time: only one request changes the password.
                throw InvalidToken();
            }

            _logger.LogInformation("Password reset completed for user {UserId}." , user.Id);

            await SessionRevocation.RevokeOlderSessionsAsync(_cache , user , now , _logger);
        }

        private static ErrorOnValidationException InvalidToken () =>
            new([ResourceErrorMessages.PASSWORD_RESET_TOKEN_INVALID]);
    }

    internal static class PasswordResetValidation
    {
        public static void ThrowIfInvalid ( FluentValidation.Results.ValidationResult result )
        {
            if ( result.IsValid == false ) {
                throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).Distinct().ToList());
            }
        }

        public static ErrorOnValidationException InvalidCode () =>
            new([ResourceErrorMessages.PASSWORD_RESET_CODE_INVALID]);
    }
}
