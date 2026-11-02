using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Security.Tokens;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Models;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Login.Refresh
{
    public class RefreshTokenUseCase : IRefreshTokenUseCase
    {
        private readonly IRedisCacheService _redisCacheService;
        private readonly IUserReadOnlyRepository _userReadOnlyRepository;
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;

        public RefreshTokenUseCase (
            IRedisCacheService redisCacheService ,
            IUserReadOnlyRepository userReadOnlyRepository ,
            IAccessTokenGenerator accessTokenGenerator ,
            IRefreshTokenGenerator refreshTokenGenerator )
        {
            _redisCacheService = redisCacheService;
            _userReadOnlyRepository = userReadOnlyRepository;
            _accessTokenGenerator = accessTokenGenerator;
            _refreshTokenGenerator = refreshTokenGenerator;
        }
        public async Task<ResponseRegisteredUserJson> Execute ( RequestRefreshTokenJson request )
        {
            var refreshTokenData =
                await _redisCacheService.GetAsync<RefreshTokenData>(
                    $"refreshToken:{request.RefreshToken}"
                );

            if ( refreshTokenData == null ) {
                throw new InvalidTokenException();
            }

            // After a password reset, sessions opened before it can no longer be renewed.
            var passwordChanged = await _redisCacheService.GetAsync<PasswordChangedData>(
                PasswordChangedData.Key(refreshTokenData.UserIdentifier));

            if ( passwordChanged != null && refreshTokenData.IssuedAt < passwordChanged.ChangedAt ) {
                await _redisCacheService.RemoveAsync($"refreshToken:{request.RefreshToken}");
                throw new InvalidTokenException();
            }

            var user = await _userReadOnlyRepository.GetByUserIdentifier(refreshTokenData.UserIdentifier);

            if( user == null ) {
                throw new NotFoundException(ResourceErrorMessages.USER_NOT_FOUND);
            }

            var newToken = _accessTokenGenerator.Generate(user);

            var newRefreshToken = _refreshTokenGenerator.Generate();

            await _redisCacheService.RemoveAsync( $"refreshToken:{request.RefreshToken}");

            var newRefreshTokenData = new RefreshTokenData
            {
                UserIdentifier = user.UserIdentifier ,
                Token = newRefreshToken ,
                ExpiresAt = DateTime.UtcNow.AddDays(7) ,
                IssuedAt = DateTime.UtcNow
            };

            await _redisCacheService.SetAsync(
                key: $"refreshToken:{newRefreshToken}" ,
                value: newRefreshTokenData ,
                expiration: TimeSpan.FromDays(7)
            );

            return new ResponseRegisteredUserJson
            {
                Name = user.Name ,
                Token = newToken ,
                RefreshToken = newRefreshToken
            };
        }
    }
}
