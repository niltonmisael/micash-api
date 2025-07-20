using MiCash.Domain.Entities;
using MiCash.Domain.Security.Tokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Infrastructure.Security.Tokens
{
    internal class JwtTokenGenerator : IAccessTokenGenerator
    {
        private readonly uint _expirationTimeMinutes;
        private readonly string _signingKey;

        public JwtTokenGenerator (uint expirationTimeMinutes, string singingKey)
        {
            _expirationTimeMinutes = expirationTimeMinutes;
            _signingKey = singingKey;
        }
        public string Generate ( User user )
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Sid, user.UserIdentifier.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddMinutes(_expirationTimeMinutes) ,
                SigningCredentials = new SigningCredentials(SecurityKey() , SecurityAlgorithms.HmacSha256Signature) ,
                Subject = new ClaimsIdentity()
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var securityToken = tokenHandler.CreateToken ( tokenDescriptor );

            return tokenHandler.WriteToken ( securityToken );

        }

        private SymmetricSecurityKey SecurityKey ()
        {
            var key = Encoding.UTF8.GetBytes ( _signingKey );

            return new SymmetricSecurityKey(key);
        }
    }
}
