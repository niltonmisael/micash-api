using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Infrastructure.DataAccess;
using MiCash.Infrastructure.DataAccess.Repositories;
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
            AddDbContext(services, configuration);
            AddRepositories (services);
        }

        private static void AddRepositories (IServiceCollection services)
        {
            services.AddScoped<IExpensesReadOnlyRepository , ExpensesRepository>();
            services.AddScoped<IExpensesWriteOnlyRepository, ExpensesRepository>();
            services.AddScoped<IExpensesUpdateOnlyRepository, ExpensesRepository>();
            services.AddScoped<IUnitOfWork , UnitOfWork>();
        }
        private static void AddDbContext ( IServiceCollection services, IConfiguration configuration )
        {
            var connectionString = configuration.GetConnectionString("Connection");
            
            services.AddDbContext<MiCashDbContext>(config => config.UseNpgsql(connectionString));
        }
    }
}
