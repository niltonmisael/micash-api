using MiCash.Application.Mappers;
using FluentValidation.Results;
using MiCash.Application.UseCases.Auth;
using MiCash.Application.UseCases.Auth.EmailVerification;
using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Security.Tokens;
using MiCash.Domain.Services.Cache;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Users.Register
{
    /// <summary>
    /// Creates the account as NOT verified and queues the confirmation code to the e-mail. The user gets a
    /// session right away (so the app can show the code screen, resend and correct the e-mail), but every other
    /// endpoint answers 403 EMAIL_NOT_VERIFIED until /verify-email. The welcome e-mail goes after the confirmation.
    /// </summary>
    public class RegisterUserUseCase : IRegisterUserUseCase
    {
        private readonly IPasswordEncripter _passwordEncripter;
        private readonly IUserReadOnlyRepository _userReadOnlyRepository;
        private readonly IUserWriteOnlyRepository _userWriteOnlyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAccessTokenGenerator _tokenGenerator;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly IRedisCacheService _redisCacheService;
        private readonly EmailVerificationCodeIssuer _verificationCodes;
        private readonly TimeProvider _time;

        public RegisterUserUseCase (
            IPasswordEncripter passwordEncripter,
            IUserReadOnlyRepository userReadOnlyRepository,
            IUserWriteOnlyRepository userWriteOnlyRepository,
            IUnitOfWork unitOfWork,
            IAccessTokenGenerator tokenGenerator,
            IRefreshTokenGenerator refreshTokenGenerator ,
            IRedisCacheService redisCacheService ,
            EmailVerificationCodeIssuer verificationCodes ,
            TimeProvider time
        )
        {
            _passwordEncripter = passwordEncripter;
            _userReadOnlyRepository = userReadOnlyRepository;
            _userWriteOnlyRepository = userWriteOnlyRepository;
            _unitOfWork = unitOfWork;
            _tokenGenerator = tokenGenerator;
            _refreshTokenGenerator = refreshTokenGenerator;
            _redisCacheService = redisCacheService;
            _verificationCodes = verificationCodes;
            _time = time;
        }

        public async Task<ResponseRegisteredUserJson> Execute ( RequestRegisterUserJson request, CancellationToken cancellationToken )
        {
            await Validate(request);

            var now = _time.GetUtcNow().UtcDateTime;

            var user = request.ToEntity();
            user.PasswordHash = _passwordEncripter.Encrypt(request.Password);
            user.UserIdentifier = Guid.NewGuid();
            user.EmailVerifiedAt = null;

            await _userWriteOnlyRepository.Add(user);

            // Saved together with the account: there is never an unverified account without a code to confirm it.
            var verification = await _verificationCodes.AddForNewUserAsync(user , now , cancellationToken);

            await _unitOfWork.Commit(cancellationToken);

            // Sent by Hangfire (with retries); if the enqueue fails the app can ask for a new code.
            _verificationCodes.Enqueue(verification);

            var (token, refreshToken) = await AuthSession.IssueAsync(user , _tokenGenerator , _refreshTokenGenerator , _redisCacheService);

            return new ResponseRegisteredUserJson
            {
                Name = user.Name ,
                Token = token ,
                RefreshToken = refreshToken ,
                EmailVerified = false
            };
        }

        private async Task Validate (RequestRegisterUserJson request)
        {
            var result = new RegisterUserValidator().Validate(request);

            var emailExist = await _userReadOnlyRepository.ExistActiveUserWithEmail(request.Email);

            if ( emailExist ) {
                result.Errors.Add(new ValidationFailure(string.Empty , ResourceErrorMessages.EMAIL_ALREADY_REGISTERED));
            }

            if ( result.IsValid == false ) {
                var errorMessages = result.Errors.Select(f => f.ErrorMessage).ToList();

                throw new ErrorOnValidationException(errorMessages);
            }
        }
    }
}
