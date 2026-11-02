namespace MiCash.Domain.Services.Cache.Models
{
    /// <summary>
    /// When the user's password was last reset. Refresh tokens issued before it are refused
    /// (refresh tokens are stored per token, without a per-user index, so they cannot be listed and deleted).
    /// Kept for the refresh token lifetime: after that every older token has expired anyway.
    /// </summary>
    public class PasswordChangedData
    {
        public DateTime ChangedAt { get; set; }

        public static string Key ( Guid userIdentifier ) => $"passwordChanged:{userIdentifier}";

        public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);
    }
}
