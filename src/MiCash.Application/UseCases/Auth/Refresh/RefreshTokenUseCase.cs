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
                ExpiresAt = DateTime.UtcNow.AddDays(7)
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
