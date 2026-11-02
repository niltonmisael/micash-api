using MiCash.Domain.Entities;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Models;
using Microsoft.Extensions.Logging;

namespace MiCash.Application.UseCases.Auth
{
    /// <summary>
    /// After a password change (reset or change-password), refresh tokens issued before it are refused by
    /// /refresh (see RefreshTokenUseCase). Access tokens already issued keep working until they expire.
    /// </summary>
    internal static class SessionRevocation
    {
        public static async Task RevokeOlderSessionsAsync ( IRedisCacheService cache , User user , DateTime changedAt , ILogger logger )
        {
            try {
                await cache.SetAsync(
                    PasswordChangedData.Key(user.UserIdentifier) ,
                    new PasswordChangedData { ChangedAt = changedAt } ,
                    PasswordChangedData.Lifetime);
            } catch ( System.Exception ex ) {
                // The password is already changed (committed); failing the request here would not undo it.
                logger.LogError(ex , "Could not revoke the sessions of user {UserId} after a password change." , user.Id);
            }
        }
    }
}
