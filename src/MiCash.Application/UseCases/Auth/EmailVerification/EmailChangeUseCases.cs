using MiCash.Communication.Requests.EmailVerification;
using MiCash.Communication.Responses;
using MiCash.Communication.Responses.EmailVerification;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Services.Jobs;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging;

namespace MiCash.Application.UseCases.Auth.EmailVerification
{
    public interface IRequestEmailChangeUseCase
    {
        Task<ResponseEmailVerificationCodeSentJson> Execute ( RequestEmailChangeJson request , CancellationToken cancellationToken );
    }

    public interface IConfirmEmailChangeUseCase
    {
        Task<ResponseUserProfileJson> Execute ( RequestConfirmEmailChangeJson request , CancellationToken cancellationToken );
    }

    /// <summary>
    /// POST api/user/email-change (verified accounts). Checks the current password and sends a code to the new
    /// address. Nothing changes in the account yet: login keeps working with the current e-mail until the code is
    /// confirmed. Same limits as the other codes (60s interval, 5 per hour); calling again resends.
    /// </summary>
    public sealed class RequestEmailChangeUseCase : IRequestEmailChangeUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IUserReadOnlyRepository _users;
        private readonly IPasswordEncripter _encripter;
        private readonly EmailVerificationCodeIssuer _issuer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _time;
        private readonly ILogger<RequestEmailChangeUseCase> _logger;

        public RequestEmailChangeUseCase (
            ILoggedUser loggedUser ,
            IUserReadOnlyRepository users ,
            IPasswordEncripter encripter ,
            EmailVerificationCodeIssuer issuer ,
            IUnitOfWork unitOfWork ,
            TimeProvider time ,
            ILogger<RequestEmailChangeUseCase> logger )
        {
            _loggedUser = loggedUser;
            _users = users;
            _encripter = encripter;
            _issuer = issuer;
            _unitOfWork = unitOfWork;
            _time = time;
            _logger = logger;
        }

        public async Task<ResponseEmailVerificationCodeSentJson> Execute ( RequestEmailChangeJson request , CancellationToken cancellationToken )
        {
            EmailVerificationValidation.ThrowIfInvalid(new EmailChangeValidator().Validate(request));

            var user = await _loggedUser.Get();

            // Same normalization as registration: the e-mail is kept as typed, without spaces around.
            var newEmail = request.NewEmail.Trim();

            if ( _encripter.Verify(request.Password , user.PasswordHash) == false ) {
                _logger.LogWarning("E-mail change refused for user {UserId}: wrong password." , user.Id);
                throw new ErrorOnValidationException([ResourceErrorMessages.PASSWORD_DIFFERENT_CURRENT_PASSWORD]);
            }

            if ( newEmail == user.Email ) {
                throw new ErrorOnValidationException([ResourceErrorMessages.EMAIL_CHANGE_SAME_AS_CURRENT]);
            }

            if ( await _users.ExistActiveUserWithEmail(newEmail) ) {
                throw new ErrorOnValidationException([ResourceErrorMessages.EMAIL_ALREADY_REGISTERED]);
            }

            var now = _time.GetUtcNow().UtcDateTime;

            await _issuer.EnsureCanRequestAsync(user.Id , now , cancellationToken , EmailVerificationPurpose.EmailChange);

            var verification = await _issuer.AddAsync(user.Id , newEmail , now , cancellationToken , EmailVerificationPurpose.EmailChange);

            try {
                await _unitOfWork.Commit(cancellationToken);
            } catch ( ConflictException ) {
                throw new TooManyRequestsException(ResourceErrorMessages.TOO_MANY_REQUESTS , now + EmailVerificationRules.ResendInterval);
            }

            _logger.LogInformation("E-mail change requested by user {UserId}; code queued to the new address." , user.Id);

            _issuer.Enqueue(verification);

            return EmailVerificationRules.CodeSent(newEmail);
        }
    }

    /// <summary>
    /// POST api/user/email-change/confirm. The right code moves the account to the new address (verified, since
    /// the code reached it) and queues a security notice to the previous one. The address is checked again here:
    /// someone may have registered it after the code was sent.
    /// </summary>
    public sealed class ConfirmEmailChangeUseCase : IConfirmEmailChangeUseCase
    {
        private readonly ILoggedUser _loggedUser;
        private readonly IUserReadOnlyRepository _userReadRepository;
        private readonly IUserUpdateOnlyRepository _userUpdateRepository;
        private readonly EmailVerificationCodeChecker _checker;
        private readonly EmailVerificationCodeIssuer _issuer;
        private readonly IBackgroundJobService _backgroundJobs;
        private readonly TimeProvider _time;
        private readonly ILogger<ConfirmEmailChangeUseCase> _logger;

        public ConfirmEmailChangeUseCase (
            ILoggedUser loggedUser ,
            IUserReadOnlyRepository userReadRepository ,
            IUserUpdateOnlyRepository userUpdateRepository ,
            EmailVerificationCodeChecker checker ,
            EmailVerificationCodeIssuer issuer ,
            IBackgroundJobService backgroundJobs ,
            TimeProvider time ,
            ILogger<ConfirmEmailChangeUseCase> logger )
        {
            _loggedUser = loggedUser;
            _userReadRepository = userReadRepository;
            _userUpdateRepository = userUpdateRepository;
            _checker = checker;
            _issuer = issuer;
            _backgroundJobs = backgroundJobs;
            _time = time;
            _logger = logger;
        }

        public async Task<ResponseUserProfileJson> Execute ( RequestConfirmEmailChangeJson request , CancellationToken cancellationToken )
        {
            EmailVerificationValidation.ThrowIfInvalid(new ConfirmEmailChangeValidator().Validate(request));

            var loggedUser = await _loggedUser.Get();
            var user = await _userUpdateRepository.GetById(loggedUser.Id);
            var now = _time.GetUtcNow().UtcDateTime;

            var verification = await _checker.CheckAsync(user , EmailVerificationPurpose.EmailChange , request.Code , now , cancellationToken);

            if ( await _userReadRepository.ExistActiveUserWithEmail(verification.Email) ) {
                verification.InvalidatedAt = now;
                await _checker.CommitOrFailAsync(cancellationToken);

                throw new ErrorOnValidationException([ResourceErrorMessages.EMAIL_ALREADY_REGISTERED]);
            }

            var previousEmail = user.Email;

            verification.VerifiedAt = now;
            user.Email = verification.Email;
            user.EmailVerifiedAt = now;
            user.UpdatedAt = now;
            _userUpdateRepository.Update(user);

            await _issuer.InvalidateOpenAsync(user.Id , now , cancellationToken , exceptId: verification.Id);

            await _checker.CommitOrFailAsync(cancellationToken);

            _logger.LogInformation("E-mail of user {UserId} changed after confirmation of request {RequestId}." , user.Id , verification.Id);

            try {
                _backgroundJobs.EnqueueEmailChangedNotice(verification.Id , previousEmail);
            } catch ( System.Exception ex ) {
                // The change is committed; only the notice is lost.
                _logger.LogError(ex , "Could not enqueue the e-mail changed notice of user {UserId}." , user.Id);
            }

            return new ResponseUserProfileJson
            {
                Name = user.Name ,
                Email = user.Email ,
                EmailVerified = true
            };
        }
    }
}
