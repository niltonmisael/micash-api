namespace MiCash.Domain.Services.Cache.Models
{
    public class RefreshTokenData
    {
        public Guid UserIdentifier { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
