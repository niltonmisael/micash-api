using MiCash.Infrastructure.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAPI.Test
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost ( IWebHostBuilder builder )
        {
            builder.UseEnvironment("Test")
                .ConfigureServices(services => {

                    var provider = services.AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();  

                    services.AddDbContext<MiCashDbContext>(config => {
                        config.UseInMemoryDatabase("InMemoryDbForTesting");
                        config.UseInternalServiceProvider(provider);
                    });
                });
        }
    }
}
