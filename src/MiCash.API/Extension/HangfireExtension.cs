using Hangfire;
using Hangfire.PostgreSql;
using MiCash.Infrastructure.Services.Jobs;

namespace MiCash.API.Extensions;

public static class HangfireExtension
{
    public static IServiceCollection AddHangfireJobs ( this IServiceCollection services , IConfiguration configuration )
    {
        services.AddHangfire(config => {
            config.UsePostgreSqlStorage(options => {
                options.UseNpgsqlConnection(
                    configuration.GetConnectionString("DefaultConnection"));
            });
        });

        services.AddHangfireServer();

        return services;
    }

    public static WebApplication RegisterRecurringJobs ( this WebApplication app )
    {
        var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();

        // Push notifications: dispatch (every minute) and Expo receipts (every 5 min).
        PushNotificationRecurringJobs.Register(recurringJobs);

        // Subscriptions: daily reconciliation with Pagar.me (safety net for lost webhooks).
        SubscriptionRecurringJobs.Register(recurringJobs);

        // Accounts: daily deletion of sign-ups that never confirmed the e-mail.
        AccountRecurringJobs.Register(recurringJobs);

        return app;
    }
}
