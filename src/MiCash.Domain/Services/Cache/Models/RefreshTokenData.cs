namespace MiCash.Domain.Services.Cache.Models
{
    public class RefreshTokenData
    {
        public Guid UserIdentifier { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }

        /// <summary>When the token was issued (UTC). Tokens stored before this field existed read as MinValue.</summary>
        public DateTime IssuedAt { get; set; }
    }
}
