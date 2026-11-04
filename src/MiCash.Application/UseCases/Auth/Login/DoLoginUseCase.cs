using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Security.Tokens;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Models;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging;

namespace MiCash.Application.UseCases.Auth.Login
{
    public class DoLoginUseCase : IDoLoginUseCase
    {
        private readonly IUserReadOnlyRepository _repository;
        private readonly IPasswordEncripter _passwordEncryption;
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<DoLoginUseCase> _logger;

        public DoLoginUseCase (
            IUserReadOnlyRepository repository, 
            IPasswordEncripter passwordEncripter, 
            IAccessTokenGenerator accessTokenGenerator,
            IRefreshTokenGenerator refreshTokenGenerator,
            IRedisCacheService redisCacheService,
            ILogger<DoLoginUseCase> logger)
        {
            _repository = repository;
            _passwordEncryption = passwordEncripter;
            _accessTokenGenerator = accessTokenGenerator;
            _refreshTokenGenerator = refreshTokenGenerator;
            _redisCacheService = redisCacheService;
            _logger = logger;

        }
        public async Task<ResponseRegisteredUserJson> Execute ( RequestLoginJson request )
        {
            
            var user = await _repository.GetUserByEmail ( request.Email );

            if ( user == null ) {
                _logger.LogWarning("Login failed. User not found for email {Email}" , request.Email);
                throw new InvalidLoginException();
            }

            var passwordMatch = _passwordEncryption.Verify(request.Password, user.PasswordHash);

            if ( passwordMatch == false ) {
                _logger.LogWarning("Invalid password for email {Email}" , request.Email);
                throw new InvalidLoginException();
            }

            var token = _accessTokenGenerator.Generate(user);

            var refreshToken = _refreshTokenGenerator.Generate();

            var refreshTokenData = new RefreshTokenData
            {
                UserIdentifier = user.UserIdentifier ,
                Token = refreshToken ,
                ExpiresAt = DateTime.UtcNow.AddDays(7) ,
                IssuedAt = DateTime.UtcNow
            };

            await _redisCacheService.SetAsync(
                key: $"refreshToken:{refreshToken}",
                value: refreshTokenData,
                expiration: TimeSpan.FromDays(7));

            _logger.LogInformation("User {Email} logged in successfully." , request.Email);
            return new ResponseRegisteredUserJson
            {
                Name = user.Name ,
                Token = token,
                RefreshToken = refreshToken ,
                // False: the app goes to the "confirm your e-mail" screen (login itself is allowed).
                EmailVerified = user.EmailVerifiedAt is not null
            };
             
        }
    }
}
