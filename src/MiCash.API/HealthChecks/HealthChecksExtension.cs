using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Text.Json;

namespace MiCash.API.HealthChecks
{
    /// <summary>
    /// GET /health: database and Redis; 200 when healthy, 503 otherwise. Private: it only answers requests from the
    /// container itself (loopback IP, as Docker's HEALTHCHECK does); from anywhere else it is a plain 404.
    /// Monitoring (Grafana) does not use it: metrics go through OpenTelemetry → collector → Prometheus.
    /// </summary>
    public static class HealthChecksExtension
    {
        private const string PATH = "/health";

        public static IServiceCollection AddApiHealthChecks ( this IServiceCollection services , IConfiguration configuration )
        {
            var healthChecks = services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>("database");

            // Redis is not registered in the in-memory test environment.
            if ( configuration.GetValue<bool>("InMemoryTest") == false ) {
                healthChecks.AddCheck<RedisHealthCheck>("redis");
            }

            return services;
        }

        /// <summary>Call before request logging (no log line every probe) and before HTTPS redirection (the probe is http).</summary>
        public static WebApplication UseApiHealthChecks ( this WebApplication app )
        {
            app.UseWhen(IsLocalRequest , branch => branch.UseHealthChecks(PATH , new HealthCheckOptions
            {
                ResponseWriter = WriteResponse
            }));

            return app;
        }

        // TCP peer address, not the Host header (a client can send any Host).
        private static bool IsLocalRequest ( HttpContext context )
        {
            var ip = context.Connection.RemoteIpAddress;

            if ( ip is null ) {
                return false;
            }

            if ( ip.IsIPv4MappedToIPv6 ) {
                ip = ip.MapToIPv4();
            }

            return IPAddress.IsLoopback(ip);
        }

        private static Task WriteResponse ( HttpContext context , HealthReport report )
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var body = new
            {
                status = report.Status.ToString() ,
                totalDuration = report.TotalDuration.TotalMilliseconds ,
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key ,
                    status = entry.Value.Status.ToString() ,
                    duration = entry.Value.Duration.TotalMilliseconds
                })
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(body , new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        }
    }
}
