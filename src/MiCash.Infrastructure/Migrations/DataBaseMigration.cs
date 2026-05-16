using MiCash.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MiCash.Infrastructure.Migrations
{
    public static class DataBaseMigration
    {
        public async static Task MigrateDatabase ( IServiceProvider serviceProvider )
        {
            var dbContext = serviceProvider.GetRequiredService<MiCashDbContext>();

            await dbContext.Database.MigrateAsync();
        }

    }
}
