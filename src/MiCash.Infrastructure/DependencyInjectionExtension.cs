using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Security.Tokens;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Infrastructure.DataAccess;
using MiCash.Infrastructure.DataAccess.Repositories;
using MiCash.Infrastructure.Extensions;
using MiCash.Infrastructure.Security.Tokens;
using MiCash.Infrastructure.Services.LoggedUser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MiCash.Infrastructure
{
    public static class DependencyInjectionExtension
    {
        public static void AddInfrastructure (this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IPasswordEncripter , Security.Cryptography.BCrypt>();
            services.AddScoped<ILoggedUser ,LoggedUser>();
            
            AddRepositories (services);
            AddToken(services , configuration);

            if ( configuration.IsTestEnvironment() == false ) {

                AddDbContext(services , configuration);
            }

        }

        private static void AddRepositories (IServiceCollection services)
        {
            services.AddScoped<IExpensesReadOnlyRepository , ExpensesRepository>();
            services.AddScoped<IExpensesWriteOnlyRepository, ExpensesRepository>();
            services.AddScoped<IExpensesUpdateOnlyRepository, ExpensesRepository>();
            services.AddScoped<IUnitOfWork , UnitOfWork>();
            services.AddScoped<IUserReadOnlyRepository , UserRepository>();
            services.AddScoped<IUserWriteOnlyRepository , UserRepository>();
        }
        private static void AddToken(this IServiceCollection services, IConfiguration configuration )
        {
            var expirationTimeMinutes = configuration.GetValue<uint>("Settings:Jwt:ExpiresMinutes");
            var signingKey = configuration.GetValue<string>("Settings:Jwt:SingingKey");

            services.AddScoped<IAccessTokenGenerator>(config => new JwtTokenGenerator(expirationTimeMinutes, signingKey!));        
        }
        private static void AddDbContext ( IServiceCollection services, IConfiguration configuration )
        {
            var connectionString = configuration.GetConnectionString("Connection");

            services.AddDbContext<MiCashDbContext>(config => config.UseNpgsql(connectionString));
        }
    }
}
