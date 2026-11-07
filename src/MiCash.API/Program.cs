using Hangfire;
using MiCash.API.Extensions;
using MiCash.API.Filters;
using MiCash.API.HealthChecks;
using MiCash.API.Middleware;
using MiCash.API.RateLimiting;
using MiCash.Infrastructure;
using MiCash.Infrastructure.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"ENV: {builder.Environment.EnvironmentName}");

builder.Host.UseSerilog(( context , configuration ) => {
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddControllers();
builder.Services.AddRouting(config => config.LowercaseUrls = true);
builder.Services.AddSwaggerDocumentation();
builder.Services.AddApiFilters();
builder.Services.AddPasswordResetRateLimit();
builder.Services.AddOpenTelemetryExtension(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddHangfireJobs(builder.Configuration);
if ( builder.Configuration.IsTestEnvironment() == false ) {
    builder.Services.AddHangfireProcessingServer();
}
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApiHealthChecks(builder.Configuration);
builder.Services.AddProxyForwardedHeaders();

var app = builder.Build();

if ( app.Environment.IsDevelopment() ) {
    app.UseSwaggerDocumentation();
    app.UseHangfireDashboard("/hangfire");
}

// Health first: it checks the real TCP peer (loopback), before forwarded headers can replace it.
app.UseApiHealthChecks();
app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
app.UseCulture();
app.UseCorsPolicy();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

if ( builder.Configuration.IsTestEnvironment() == false ) {
    await app.MigrateDatabaseAsync();
    app.RegisterRecurringJobs();
}

app.Run();

public partial class Program { }
