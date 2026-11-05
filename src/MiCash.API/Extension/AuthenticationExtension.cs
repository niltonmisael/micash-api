using MiCash.API.Token;
using MiCash.Domain.Security.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MiCash.API.Extensions;

public static class AuthenticationExtension
{
    public static IServiceCollection AddJwtAuthentication ( this IServiceCollection services , IConfiguration configuration )
    {
        services.AddScoped<ITokenProvider , HttpContextTokenValue>();
        services.AddHttpContextAccessor();

        var signingKey = configuration.GetValue<string>("Settings:Jwt:SingingKey");

        services.AddAuthentication(config => {
            config.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            config.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(config => {
            config.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false ,
                ValidateAudience = false ,
                ClockSkew = new TimeSpan(0) ,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey!))
            };
        });

        return services;
    }
}
