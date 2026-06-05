using Microsoft.Extensions.Configuration;

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
