using AutoMapper;
using FluentValidation.Results;
using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Security.Tokens;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Models;
using MiCash.Domain.Services.Jobs;
using Microsoft.Extensions.Logging;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Users.Register
{
    public class RegisterUserUseCase : IRegisterUserUseCase
    {
        private readonly IMapper _mapper;
        private readonly IPasswordEncripter _passwordEncripter;
        private readonly IUserReadOnlyRepository _userReadOnlyRepository;
        private readonly IUserWriteOnlyRepository _userWriteOnlyRepository;
        private readonly IUnitOfWork _unitOfWork; 
        private readonly IAccessTokenGenerator _tokenGenerator;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly IRedisCacheService _redisCacheService;
        private readonly IBackgroundJobService _backgroundJobs;
        private readonly ILogger<RegisterUserUseCase> _logger;

        public RegisterUserUseCase (
            IMapper mapper, 
            IPasswordEncripter passwordEncripter, 
            IUserReadOnlyRepository userReadOnlyRepository,
            IUserWriteOnlyRepository userWriteOnlyRepository, 
            IUnitOfWork unitOfWork, 
            IAccessTokenGenerator tokenGenerator,
            IRefreshTokenGenerator refreshTokenGenerator ,
            IRedisCacheService redisCacheService ,
            IBackgroundJobService backgroundJobs ,
            ILogger<RegisterUserUseCase> logger
        )
        {
            _backgroundJobs = backgroundJobs;
            _logger = logger;
            _mapper = mapper;
            _passwordEncripter = passwordEncripter;
            _userReadOnlyRepository = userReadOnlyRepository;
            _userWriteOnlyRepository = userWriteOnlyRepository;
            _unitOfWork = unitOfWork;  
            _tokenGenerator = tokenGenerator;
            _refreshTokenGenerator = refreshTokenGenerator;
            _redisCacheService = redisCacheService;
        }

        public async Task<ResponseRegisteredUserJson> Execute ( RequestRegisterUserJson request, CancellationToken cancellationToken )
        {
            await Validate(request);

            var user = _mapper.Map<Domain.Entities.User>(request);
            user.PasswordHash = _passwordEncripter.Encrypt(request.Password);
            user.UserIdentifier = Guid.NewGuid();

            await _userWriteOnlyRepository.Add(user);

            await _unitOfWork.Commit(cancellationToken);

            EnqueueWelcomeEmail(user.Id);

            var token = _tokenGenerator.Generate(user);

            var refreshToken = _refreshTokenGenerator.Generate();

            var refreshTokenData = new RefreshTokenData
            {
                UserIdentifier = user.UserIdentifier,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7) ,
                IssuedAt = DateTime.UtcNow
            };

            await _redisCacheService.SetAsync(
                key: $"refreshToken:{refreshToken}" ,
                value: refreshTokenData,
                expiration: TimeSpan.FromDays(7)
            );

            return new ResponseRegisteredUserJson
            {
                Name = user.Name ,
                Token = token ,
                RefreshToken = refreshToken ,
            };
        }

        // Sent by Hangfire (with retries): the account is already saved and a failure here never undoes it.
        private void EnqueueWelcomeEmail ( long userId )
        {
            try {
                _backgroundJobs.EnqueueWelcomeEmail(userId);
            } catch ( System.Exception ex ) {
                _logger.LogError(ex , "Could not enqueue the welcome e-mail of user {UserId}." , userId);
            }
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
