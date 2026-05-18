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

                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;

                        options.EnrichWithHttpRequest = ( activity , request ) =>
                        {
                            activity.SetTag("http.route" , request.Path);
                        };
                    })

                    .AddHttpClientInstrumentation()

                    .AddSqlClientInstrumentation()

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

                    .AddOtlpExporter(options => {
                        options.Endpoint =
                            new Uri(otlpEndpoint!);
                    });
            });

        return services;
    }
}