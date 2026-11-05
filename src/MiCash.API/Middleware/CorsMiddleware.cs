namespace MiCash.API.Middleware
{
    /// <summary>
    /// CORS policy for the web front end. Allowed origins come from "Cors:AllowedOrigins".
    /// In the in-memory test environment no policy is registered (same behavior as before).
    /// </summary>
    public static class CorsMiddleware
    {
        public const string POLICY = "AllowFrontend";

        public static IServiceCollection AddCorsPolicy ( this IServiceCollection services , IConfiguration configuration )
        {
            var allowedOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>();

            return services.AddCors(options => {
                var isInMemoryTest = configuration.GetValue<bool>("InMemoryTest");

                if ( isInMemoryTest ) {
                    return;
                }

                options.AddPolicy(POLICY , policy => {
                    policy
                        .WithOrigins(allowedOrigins!)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
        }

        public static IApplicationBuilder UseCorsPolicy ( this IApplicationBuilder app )
        {
            return app.UseCors(POLICY);
        }
    }
}
