using MiCash.Domain.Security.Tokens;
using System.Security.Cryptography;

namespace MiCash.Infrastructure.Security.Tokens
{
    public class RefreshTokenGenerator : IRefreshTokenGenerator
    {
        public string Generate ()
        {
            return Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(64));
        }
    }
}
