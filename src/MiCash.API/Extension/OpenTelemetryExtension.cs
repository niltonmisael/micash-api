using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MiCash.API.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryExtension(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var isInMemoryTest = configuration.GetValue<bool>("InMemoryTest");

        if (isInMemoryTest)
            return services;

        var serviceName = configuration["OpenTelemetry:ServiceName"];

        if (string.IsNullOrWhiteSpace(serviceName))
            serviceName = "micash-api";

        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];

        var hasOtlpEndpoint = !string.IsNullOrWhiteSpace(otlpEndpoint);

        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource
                    .AddService(
                        serviceName: serviceName,
                        serviceVersion: "1.0.0")
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["environment"] =
                            configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production"
                    });
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddNpgsql();

                if (hasOtlpEndpoint)
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint!);
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddNpgsqlInstrumentation();

                if (hasOtlpEndpoint)
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint!);
                    });
                }
            });

        return services;
    }
}