using MiCash.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Infrastructure.Migrations
{
    public static class DataBaseMigration
    {
        public async static Task MigrationDataBase (IServiceProvider serviceProvider)
        {
            var dbContext = serviceProvider.GetRequiredService<MiCashDbContext> ();

            await dbContext.Database.MigrateAsync();
        }
    }
}
