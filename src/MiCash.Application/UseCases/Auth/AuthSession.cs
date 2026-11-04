using MiCash.Domain.Entities;
using MiCash.Domain.Security.Tokens;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Models;

namespace MiCash.Application.UseCases.Auth
{
    /// <summary>Access token + refresh token (stored in Redis for 7 days), as login and registration hand out.</summary>
    internal static class AuthSession
    {
        public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

        public static async Task<(string AccessToken, string RefreshToken)> IssueAsync (
            User user ,
            IAccessTokenGenerator accessTokenGenerator ,
            IRefreshTokenGenerator refreshTokenGenerator ,
            IRedisCacheService cache )
        {
            var accessToken = accessTokenGenerator.Generate(user);
            var refreshToken = refreshTokenGenerator.Generate();

            await cache.SetAsync(
                key: $"refreshToken:{refreshToken}" ,
                value: new RefreshTokenData
                {
                    UserIdentifier = user.UserIdentifier ,
                    Token = refreshToken ,
                    ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime) ,
                    IssuedAt = DateTime.UtcNow
                } ,
                expiration: RefreshTokenLifetime);

            return (accessToken, refreshToken);
        }
    }
}
