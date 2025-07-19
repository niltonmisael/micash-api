using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Infrastructure.Extensions
{
    public static class ConfigurationsExtension
    {
        public static bool IsTestEnvironment( this IConfiguration configuration )
        {
            return configuration.GetValue<bool>("InMemoryTest");
        }
    }
}
