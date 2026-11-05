using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MiCash.API.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryExtension (
        this IServiceCollection services ,
        IConfiguration configuration )
    {
        var isInMemoryTest =
        configuration.GetValue<bool>("InMemoryTest");

        if ( isInMemoryTest ) {
            return services;
        }

        var serviceName =
            configuration["OpenTelemetry:ServiceName"];

        var otlpEndpoint =
            configuration["OpenTelemetry:OtlpEndpoint"];

        services.AddOpenTelemetry()

            .ConfigureResource(resource => {
                resource
                    .AddService(
                        serviceName: serviceName! ,
                        serviceVersion: "1.0.0")
                    .AddAttributes(new Dictionary<string , object>
                    {
                        ["environment"] =
                            configuration["ASPNETCORE_ENVIRONMENT"]
                            ?? "Development"
                    });
            })

            .WithTracing(tracing => {
                tracing

                    // http.route is filled by ASP.NET Core with the route template (/api/expenses/{id}).
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;

                        // Docker's HEALTHCHECK calls /health every 30s: no trace for it.
                        options.Filter = context =>
                            context.Request.Path.StartsWithSegments("/health") == false;
                    })

                    .AddHttpClientInstrumentation()

                    // PostgreSQL queries as child spans of the request.
                    .AddNpgsql()

                    .AddOtlpExporter(options => {
                        options.Endpoint =
                            new Uri(otlpEndpoint!);
                    });
            })

            .WithMetrics(metrics => {
                metrics

                    .AddAspNetCoreInstrumentation()

                    .AddHttpClientInstrumentation()

                    .AddRuntimeInstrumentation()

                    // PostgreSQL metrics (command duration, connection pool).
                    .AddNpgsqlInstrumentation()

                    .AddOtlpExporter(options => {
                        options.Endpoint =
                            new Uri(otlpEndpoint!);
                    });
            });

        return services;
    }
}