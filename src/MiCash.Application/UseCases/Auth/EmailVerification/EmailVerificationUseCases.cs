using MiCash.Communication.Requests.EmailVerification;
using MiCash.Communication.Responses.EmailVerification;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.EmailVerifications;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Security.Tokens;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Jobs;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Domain.Services.Plans;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging;

namespace MiCash.Application.UseCases.Auth.EmailVerification
{
    public interface IVerifyEmailUseCase
    {
        Task<ResponseEmailVerifiedJson> Execute ( RequestVerifyEmailJson request , CancellationToken cancellationToken );
    }

    public interface IResendEmailVerificationCodeUseCase
    {
        Task<ResponseEmailVerificationCodeSentJson> Execute ( CancellationToken cancellationToken );
    }

    public interface IChangeVerificationEmailUseCase
    {
        Task<ResponseEmailVerificationCodeSentJson> Execute ( RequestChangeVerificationEmailJson request , CancellationToken cancellationToken );
    }

    /// <summary>
    /// POST /verify-email (logged in, not verified yet). The right code confirms the account, returns new
    /// session tokens (the current access token still says "not verified") and queues the welcome e-mail.
    /// Every failure (no code, wrong, expired, too many attempts) has the same message.
    /// Calling it again after the confirmation just returns new tokens.
    /// </summary>
    public sealed class VerifyEmailUseCase : IVerifyEmailUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IUserUpdateOnlyRepository _users;
        private readonly EmailVerificationCodeChecker _checker;
        private readonly EmailVerificationCodeIssuer _issuer;
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly IRedisCacheService _cache;
        private readonly IUserPlanService _plans;
        private readonly IBackgroundJobService _backgroundJobs;
        private readonly TimeProvider _time;
        private readonly ILogger<VerifyEmailUseCase> _logger;

        public VerifyEmailUseCase (
            ILoggedUser loggedUser ,
            IUserUpdateOnlyRepository users ,
            EmailVerificationCodeChecker checker ,
            EmailVerificationCodeIssuer issuer ,
            IAccessTokenGenerator accessTokenGenerator ,
            IRefreshTokenGenerator refreshTokenGenerator ,
            IRedisCacheService cache ,
            IUserPlanService plans ,
            IBackgroundJobService backgroundJobs ,
            TimeProvider time ,
            ILogger<VerifyEmailUseCase> logger )
        {
            _loggedUser = loggedUser;
            _users = users;
            _checker = checker;
            _issuer = issuer;
            _accessTokenGenerator = accessTokenGenerator;
            _refreshTokenGenerator = refreshTokenGenerator;
            _cache = cache;
            _plans = plans;
            _backgroundJobs = backgroundJobs;
            _time = time;
            _logger = logger;
        }

        public async Task<ResponseEmailVerifiedJson> Execute ( RequestVerifyEmailJson request , CancellationToken cancellationToken )
        {
            var loggedUser = await _loggedUser.Get();
            var user = await _users.GetById(loggedUser.Id);

            // Double tap / retry after a lost response: already confirmed, nothing to check.
            if ( user.EmailVerifiedAt is not null ) {
                return await VerifiedAsync(user , cancellationToken);
            }

            EmailVerificationValidation.ThrowIfInvalid(new VerifyEmailValidator().Validate(request));

            var now = _time.GetUtcNow().UtcDateTime;

            // A code sent to an address the user corrected afterwards proves nothing (see StillApplies).
            var verification = await _checker.CheckAsync(user , EmailVerificationPurpose.SignUp , request.Code , now , cancellationToken);

            verification.VerifiedAt = now;
            user.EmailVerifiedAt = now;
            user.UpdatedAt = now;
            _users.Update(user);

            await _issuer.InvalidateOpenAsync(user.Id , now , cancellationToken , exceptId: verification.Id);

            await _checker.CommitOrFailAsync(cancellationToken);

            _logger.LogInformation("E-mail verified for user {UserId}." , user.Id);

            EnqueueWelcomeEmail(user.Id);

            return await VerifiedAsync(user , cancellationToken);
        }

        private async Task<ResponseEmailVerifiedJson> VerifiedAsync ( User user , CancellationToken cancellationToken )
        {
            var (accessToken, refreshToken) = await AuthSession.IssueAsync(user , _accessTokenGenerator , _refreshTokenGenerator , _cache);

            var limits = await _plans.GetLimitsAsync(user , cancellationToken);

            return new ResponseEmailVerifiedJson
            {
                Name = user.Name ,
                Token = accessToken ,
                RefreshToken = refreshToken ,
                EmailVerified = true ,
                ShowPremiumOffer = limits.IsPremium == false
            };
        }

        // The account is already confirmed; a Hangfire failure only loses the welcome e-mail.
        private void EnqueueWelcomeEmail ( long userId )
        {
            try {
                _backgroundJobs.EnqueueWelcomeEmail(userId);
            } catch ( System.Exception ex ) {
                _logger.LogError(ex , "Could not enqueue the welcome e-mail of user {UserId}." , userId);
            }
        }
    }

    /// <summary>POST /resend-email-verification-code: a new code to the account e-mail (60s interval, 5 per hour).</summary>
    public sealed class ResendEmailVerificationCodeUseCase : IResendEmailVerificationCodeUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly EmailVerificationCodeIssuer _issuer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _time;
        private readonly ILogger<ResendEmailVerificationCodeUseCase> _logger;

        public ResendEmailVerificationCodeUseCase (
            ILoggedUser loggedUser ,
            EmailVerificationCodeIssuer issuer ,
            IUnitOfWork unitOfWork ,
            TimeProvider time ,
            ILogger<ResendEmailVerificationCodeUseCase> logger )
        {
            _loggedUser = loggedUser;
            _issuer = issuer;
            _unitOfWork = unitOfWork;
            _time = time;
            _logger = logger;
        }

        public async Task<ResponseEmailVerificationCodeSentJson> Execute ( CancellationToken cancellationToken )
        {
            var user = await _loggedUser.Get();

            if ( user.EmailVerifiedAt is not null ) {
                throw new ErrorOnValidationException([ResourceErrorMessages.EMAIL_ALREADY_VERIFIED]);
            }

            var now = _time.GetUtcNow().UtcDateTime;

            await _issuer.EnsureCanRequestAsync(user.Id , now , cancellationToken);

            var verification = await _issuer.AddAsync(user.Id , user.Email , now , cancellationToken);

            try {
                await _unitOfWork.Commit(cancellationToken);
            } catch ( ConflictException ) {
                // A parallel resend changed the open requests: that one sends the code.
                _logger.LogInformation("E-mail verification resend of user {UserId} superseded by a parallel request." , user.Id);
                return EmailVerificationRules.CodeSent(user.Email);
            }

            _issuer.Enqueue(verification);

            return EmailVerificationRules.CodeSent(user.Email);
        }
    }

    /// <summary>
    /// PUT /email-verification/email: corrects a mistyped address before it is confirmed and sends a code to the
    /// new one (same limits as the resend). After the confirmation the e-mail is changed in the profile.
    /// </summary>
    public sealed class ChangeVerificationEmailUseCase : IChangeVerificationEmailUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IUserReadOnlyRepository _userReadRepository;
        private readonly IUserUpdateOnlyRepository _userUpdateRepository;
        private readonly EmailVerificationCodeIssuer _issuer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _time;
        private readonly ILogger<ChangeVerificationEmailUseCase> _logger;

        public ChangeVerificationEmailUseCase (
            ILoggedUser loggedUser ,
            IUserReadOnlyRepository userReadRepository ,
            IUserUpdateOnlyRepository userUpdateRepository ,
            EmailVerificationCodeIssuer issuer ,
            IUnitOfWork unitOfWork ,
            TimeProvider time ,
            ILogger<ChangeVerificationEmailUseCase> logger )
        {
            _loggedUser = loggedUser;
            _userReadRepository = userReadRepository;
            _userUpdateRepository = userUpdateRepository;
            _issuer = issuer;
            _unitOfWork = unitOfWork;
            _time = time;
            _logger = logger;
        }

        public async Task<ResponseEmailVerificationCodeSentJson> Execute ( RequestChangeVerificationEmailJson request , CancellationToken cancellationToken )
        {
            EmailVerificationValidation.ThrowIfInvalid(new ChangeVerificationEmailValidator().Validate(request));

            var loggedUser = await _loggedUser.Get();
            var user = await _userUpdateRepository.GetById(loggedUser.Id);

            if ( user.EmailVerifiedAt is not null ) {
                throw new ErrorOnValidationException([ResourceErrorMessages.EMAIL_ALREADY_VERIFIED]);
            }

            // Same normalization as registration: the e-mail is kept as typed, without spaces around.
            var email = request.Email.Trim();
            var now = _time.GetUtcNow().UtcDateTime;

            if ( email != user.Email ) {
                if ( await _userReadRepository.ExistActiveUserWithEmail(email) ) {
                    throw new ErrorOnValidationException([ResourceErrorMessages.EMAIL_ALREADY_REGISTERED]);
                }
            }

            await _issuer.EnsureCanRequestAsync(user.Id , now , cancellationToken);

            user.Email = email;
            user.UpdatedAt = now;
            _userUpdateRepository.Update(user);

            var verification = await _issuer.AddAsync(user.Id , email , now , cancellationToken);

            try {
                await _unitOfWork.Commit(cancellationToken);
            } catch ( ConflictException ) {
                _logger.LogInformation("E-mail correction of user {UserId} superseded by a parallel request." , user.Id);
                throw new TooManyRequestsException(ResourceErrorMessages.TOO_MANY_REQUESTS , now + EmailVerificationRules.ResendInterval);
            }

            _logger.LogInformation("Unverified user {UserId} corrected the e-mail; a new code was requested." , user.Id);

            _issuer.Enqueue(verification);

            return EmailVerificationRules.CodeSent(email);
        }
    }

    internal static class EmailVerificationValidation
    {
        public static void ThrowIfInvalid ( FluentValidation.Results.ValidationResult result )
        {
            if ( result.IsValid == false ) {
                throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).Distinct().ToList());
            }
        }
    }
}
