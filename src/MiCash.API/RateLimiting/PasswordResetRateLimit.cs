using MiCash.Communication.Responses;
using MiCash.Exception;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace MiCash.API.RateLimiting
{
    /// <summary>
    /// Per-IP limit of the anonymous password reset endpoints (ASP.NET Core rate limiter, no extra package).
    /// It complements the per-account limits of the use cases: those protect one account, this one stops
    /// a single client from spraying many e-mails or guesses. 429 with the usual { errorMessages } body.
    /// Behind a reverse proxy, configure forwarded headers first or every client shares the proxy's IP.
    /// </summary>
    public static class PasswordResetRateLimit
    {
        public const string POLICY = "password-reset";

        public const int PERMIT_LIMIT = 20;

        public static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

        public static IServiceCollection AddPasswordResetRateLimit ( this IServiceCollection services )
        {
            return services.AddRateLimiter(options => {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(POLICY , context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown" ,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = PERMIT_LIMIT ,
                        Window = Window ,
                        QueueLimit = 0
                    }));

                options.OnRejected = async ( context , cancellationToken ) => {
                    if ( context.Lease.TryGetMetadata(MetadataName.RetryAfter , out var retryAfter) ) {
                        context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                    }

                    await context.HttpContext.Response.WriteAsJsonAsync(
                        new ResponseErrorJson(ResourceErrorMessages.TOO_MANY_REQUESTS) , cancellationToken);
                };
            });
        }
    }
}
