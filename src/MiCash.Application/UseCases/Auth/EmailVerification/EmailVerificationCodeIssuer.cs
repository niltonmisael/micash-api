using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.EmailVerifications;
using MiCash.Domain.Services.Jobs;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace MiCash.Application.UseCases.Auth.EmailVerification
{
    /// <summary>
    /// Creates verification requests (registration, resend, e-mail correction) and hands them to the e-mail job.
    /// The caller commits between <c>Add…</c> and <see cref="Enqueue"/>: the job needs the saved request id.
    /// </summary>
    public sealed class EmailVerificationCodeIssuer
    {
        private readonly IEmailVerificationReadOnlyRepository _readRepository;
        private readonly IEmailVerificationWriteOnlyRepository _writeRepository;
        private readonly IEmailVerificationUpdateOnlyRepository _updateRepository;
        private readonly IBackgroundJobService _backgroundJobs;
        private readonly ILogger<EmailVerificationCodeIssuer> _logger;

        public EmailVerificationCodeIssuer (
            IEmailVerificationReadOnlyRepository readRepository ,
            IEmailVerificationWriteOnlyRepository writeRepository ,
            IEmailVerificationUpdateOnlyRepository updateRepository ,
            IBackgroundJobService backgroundJobs ,
            ILogger<EmailVerificationCodeIssuer> logger )
        {
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _updateRepository = updateRepository;
            _backgroundJobs = backgroundJobs;
            _logger = logger;
        }

        /// <summary>
        /// 429 (with retryAt) inside the resend interval of the same purpose, or above the hourly limit (all
        /// purposes together).
        /// </summary>
        public async Task EnsureCanRequestAsync (
            long userId , DateTime now , CancellationToken cancellationToken ,
            EmailVerificationPurpose purpose = EmailVerificationPurpose.SignUp )
        {
            var latest = await _readRepository.GetLatestCreatedAtAsync(userId , purpose , cancellationToken);

            if ( latest is not null && now - latest.Value < EmailVerificationRules.ResendInterval ) {
                var retryAt = latest.Value + EmailVerificationRules.ResendInterval;
                var seconds = (int)Math.Ceiling((retryAt - now).TotalSeconds);

                throw new TooManyRequestsException(
                    string.Format(CultureInfo.CurrentCulture , ResourceErrorMessages.EMAIL_VERIFICATION_RESEND_WAIT , seconds) ,
                    retryAt);
            }

            var lastHour = await _readRepository.CountCreatedSinceAsync(userId , now.AddHours(-1) , cancellationToken);

            if ( lastHour >= EmailVerificationRules.MAX_REQUESTS_PER_HOUR ) {
                _logger.LogWarning("E-mail verification code refused for user {UserId}: hourly limit reached." , userId);

                throw new TooManyRequestsException(ResourceErrorMessages.TOO_MANY_REQUESTS , now.AddHours(1));
            }
        }

        /// <summary>First code of an account being registered (not saved yet: linked by the navigation).</summary>
        public async Task<EmailVerificationRequest> AddForNewUserAsync ( User newUser , DateTime now , CancellationToken cancellationToken )
        {
            var request = NewRequest(newUser.Email , now);
            request.User = newUser;

            await _writeRepository.AddAsync(request , cancellationToken);

            return request;
        }

        /// <summary>A new code for an existing account; the previous open codes (of any purpose) stop working.</summary>
        public async Task<EmailVerificationRequest> AddAsync (
            long userId , string email , DateTime now , CancellationToken cancellationToken ,
            EmailVerificationPurpose purpose = EmailVerificationPurpose.SignUp )
        {
            await InvalidateOpenAsync(userId , now , cancellationToken);

            var request = NewRequest(email , now);
            request.UserId = userId;
            request.Purpose = purpose;

            await _writeRepository.AddAsync(request , cancellationToken);

            return request;
        }

        public async Task InvalidateOpenAsync ( long userId , DateTime now , CancellationToken cancellationToken , long? exceptId = null )
        {
            foreach ( var open in await _updateRepository.GetOpenByUserAsync(userId , cancellationToken) ) {
                if ( open.Id != exceptId ) {
                    open.InvalidatedAt = now;
                }
            }
        }

        /// <summary>Never throws: the request is saved and the user can ask again after the resend interval.</summary>
        public void Enqueue ( EmailVerificationRequest request )
        {
            try {
                _backgroundJobs.EnqueueEmailVerificationEmail(request.Id);
            } catch ( System.Exception ex ) {
                _logger.LogError(ex , "Could not enqueue the e-mail verification code of request {RequestId}." , request.Id);
            }
        }

        private static EmailVerificationRequest NewRequest ( string email , DateTime now ) => new()
        {
            Email = email ,
            CreatedAt = now ,
            ExpiresAt = now + EmailVerificationRules.CodeValidity
        };
    }
}
