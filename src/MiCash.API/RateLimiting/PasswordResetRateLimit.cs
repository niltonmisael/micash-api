using MiCash.Communication.Responses;
using MiCash.Exception;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace MiCash.API.RateLimiting
{
    /// <summary>
    /// Per-IP limits of the anonymous endpoints (ASP.NET Core rate limiter, no extra package).
    /// They complement the per-account limits of the use cases: those protect one account, these stop
    /// a single client from spraying many e-mails or password guesses. 429 with the usual { errorMessages } body.
    /// Behind a reverse proxy, configure forwarded headers first or every client shares the proxy's IP.
    /// </summary>
    public static class PasswordResetRateLimit
    {
        public const string POLICY = "password-reset";

        public const string EMAIL_VERIFICATION_POLICY = "email-verification";

        /// <summary>Login: blocks brute force and credential stuffing from one IP.</summary>
        public const string LOGIN_POLICY = "login";

        /// <summary>Refresh: the token can't be guessed, this only caps abuse of the endpoint.</summary>
        public const string REFRESH_POLICY = "refresh";

        public const int PERMIT_LIMIT = 20;

        public static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

        // Room for a few typos and for users sharing one IP (mobile carrier NAT, office Wi-Fi).
        public const int LOGIN_PERMIT_LIMIT = 20;

        public static readonly TimeSpan LoginWindow = TimeSpan.FromMinutes(5);

        public const int REFRESH_PERMIT_LIMIT = 60;

        public static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(5);

        public static IServiceCollection AddPasswordResetRateLimit ( this IServiceCollection services )
        {
            return services.AddRateLimiter(options => {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                AddPerIpPolicy(options , POLICY , PERMIT_LIMIT , Window);

                // Registration and e-mail verification (each one may send a code): same per-IP limit,
                // own counter, on top of the per-account limits of the use cases.
                AddPerIpPolicy(options , EMAIL_VERIFICATION_POLICY , PERMIT_LIMIT , Window);

                AddPerIpPolicy(options , LOGIN_POLICY , LOGIN_PERMIT_LIMIT , LoginWindow);

                AddPerIpPolicy(options , REFRESH_POLICY , REFRESH_PERMIT_LIMIT , RefreshWindow);

                options.OnRejected = async ( context , cancellationToken ) => {
                    if ( context.Lease.TryGetMetadata(MetadataName.RetryAfter , out var retryAfter) ) {
                        context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                    }

                    await context.HttpContext.Response.WriteAsJsonAsync(
                        new ResponseErrorJson(ResourceErrorMessages.TOO_MANY_REQUESTS) , cancellationToken);
                };
            });
        }

        private static void AddPerIpPolicy ( RateLimiterOptions options , string policy , int permitLimit , TimeSpan window )
        {
            options.AddPolicy(policy , context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown" ,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit ,
                    Window = window ,
                    QueueLimit = 0
                }));
        }
    }
}
