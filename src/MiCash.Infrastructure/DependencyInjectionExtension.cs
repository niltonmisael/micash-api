using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Categories;
using MiCash.Domain.Repositories.Conversations;
using MiCash.Domain.Repositories.Dashboards;
using MiCash.Domain.Repositories.Expenses;
using MiCash.Domain.Repositories.FinancialGoals;
using MiCash.Domain.Repositories.SavingsGoals;
using MiCash.Domain.Repositories.Incomes;
using MiCash.Domain.Repositories.PasswordResets;
using MiCash.Domain.Repositories.PaymentLinks;
using MiCash.Domain.Repositories.Plans;
using MiCash.Domain.Repositories.PushNotifications;
using MiCash.Domain.Repositories.Subscriptions;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Repositories.WebHook;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Security.Tokens;
using MiCash.Domain.Security.WebHook;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Email;
using MiCash.Domain.Services.IA;
using MiCash.Domain.Services.Jobs;
using MiCash.Domain.Services.LoggedUser;
using MiCash.Domain.Services.Payments;
using MiCash.Domain.Services.PushNotifications;
using MiCash.Infrastructure.AI.Services;
using MiCash.Infrastructure.DataAccess;
using MiCash.Infrastructure.DataAccess.Repositories;
using MiCash.Infrastructure.Extensions;
using MiCash.Infrastructure.Resilience.Policies;
using MiCash.Infrastructure.Security.Tokens;
using MiCash.Infrastructure.Security.WebHook;
using MiCash.Infrastructure.Services.Cache.Redis;
using MiCash.Infrastructure.Services.Email;
using MiCash.Infrastructure.Services.IA.Configuration;
using MiCash.Infrastructure.Services.Jobs;
using MiCash.Infrastructure.Services.LoggedUser;
using MiCash.Infrastructure.Services.Payments;
using MiCash.Infrastructure.Services.PushNotifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Resend;
using StackExchange.Redis;

namespace MiCash.Infrastructure
{
    public static class DependencyInjectionExtension
    {
        public static void AddInfrastructure ( this IServiceCollection services , IConfiguration configuration )
        {
            services.AddScoped<IPasswordEncripter , Security.Cryptography.BCrypt>();
            services.AddScoped<ILoggedUser , LoggedUser>();

            AddRepositories(services);
            AddPayments(services , configuration);
            AddRedis(services , configuration);
            AddToken(services , configuration);
            AddIA(services , configuration);
            AddPushNotifications(services , configuration);
            AddEmail(services , configuration);

            if ( configuration.IsTestEnvironment() == false ) {

                AddDbContext(services , configuration);
            }

        }

        private static void AddRepositories ( IServiceCollection services )
        {
            services.AddScoped<IExpensesReadOnlyRepository , ExpensesRepository>();
            services.AddScoped<IExpensesWriteOnlyRepository , ExpensesRepository>();
            services.AddScoped<IExpensesUpdateOnlyRepository , ExpensesRepository>();
            services.AddScoped<IUnitOfWork , UnitOfWork>();
            services.AddScoped<IUserReadOnlyRepository , UserRepository>();
            services.AddScoped<IUserWriteOnlyRepository , UserRepository>();
            services.AddScoped<IUserUpdateOnlyRepository , UserRepository>();
            services.AddScoped<ICategoriesWriteOnlyRepository , CategoryRepository>();
            services.AddScoped<ICategoriesReadOnlyRepository , CategoryRepository>();
            services.AddScoped<ICategoriesUpdateOnlyRepository , CategoryRepository>();
            services.AddScoped<IIncomesUpdateOnlyRepository , IncomeRepository>();
            services.AddScoped<IIncomesWriteOnlyRepository , IncomeRepository>();
            services.AddScoped<IIncomesReadOnlyRepository , IncomeRepository>();
            services.AddScoped<IDashboardReadOnlyRepository, DashboardRepository>();
            services.AddScoped<ICategoriesReadOnlyRepository, CategoryRepository>();
            services.AddScoped<ICategoriesWriteOnlyRepository, CategoryRepository>();
            services.AddScoped<ICategoriesUpdateOnlyRepository, CategoryRepository>();
            services.AddScoped<IPlansReadOnlyRepository, PlanRepository>();
            services.AddScoped<ISubscriptionsReadOnlyRepository, SubscriptionRepository>();
            services.AddScoped<ISubscriptionsUpdateOnlyRepository, SubscriptionRepository>();
            services.AddScoped<ISubscriptionsWriteOnlyRepository, SubscriptionRepository>();
            services.AddScoped<IWebhookAuthenticator ,WebhookAuthenticator>();
            services.AddScoped<IPaymentLinkRepository, PaymentLinkRepository>();
            services.AddScoped<IBackgroundJobService ,HangfireBackgroundJobService>();
            services.AddScoped<IProcessedWebhookRepository, ProcessedWebhookRepository>();
            services.AddScoped<IFinancialGoalReadOnlyRepository, FinancialGoalRepository>();
            services.AddScoped<IFinancialGoalWriteOnlyRepository, FinancialGoalRepository>();
            services.AddScoped<IFinancialGoalUpdateOnlyRepository, FinancialGoalRepository>();
            services.AddScoped<IConversationMessageReadOnlyRepository, ConversationRepository>();
            services.AddScoped<IConversationMessageWriteOnlyRepository, ConversationRepository>();
            services.AddScoped<IConversationReadOnlyRepository, ConversationRepository>();
            services.AddScoped<IConversationWriteOnlyRepository, ConversationRepository>();
            services.AddScoped<ISavingsGoalReadOnlyRepository , SavingsGoalRepository>();
            services.AddScoped<ISavingsGoalWriteOnlyRepository , SavingsGoalRepository>();
            services.AddScoped<ISavingsGoalUpdateOnlyRepository , SavingsGoalRepository>();
            services.AddScoped<IPushDeviceReadOnlyRepository , PushNotificationRepository>();
            services.AddScoped<IPushDeviceWriteOnlyRepository , PushNotificationRepository>();
            services.AddScoped<IPushDeviceUpdateOnlyRepository , PushNotificationRepository>();
            services.AddScoped<IPushNotificationReadOnlyRepository , PushNotificationRepository>();
            services.AddScoped<IPushNotificationWriteOnlyRepository , PushNotificationRepository>();
            services.AddScoped<IPushDeliveryUpdateOnlyRepository , PushNotificationRepository>();
            services.AddScoped<INotificationCenterReadOnlyRepository , PushNotificationRepository>();
            services.AddScoped<INotificationCenterUpdateOnlyRepository , PushNotificationRepository>();
            services.AddScoped<IPasswordResetReadOnlyRepository , PasswordResetRepository>();
            services.AddScoped<IPasswordResetWriteOnlyRepository , PasswordResetRepository>();
            services.AddScoped<IPasswordResetUpdateOnlyRepository , PasswordResetRepository>();
        }
        private static void AddToken ( this IServiceCollection services , IConfiguration configuration )
        {
            var expirationTimeMinutes = configuration.GetValue<uint>("Settings:Jwt:ExpiresMinutes");
            var signingKey = configuration.GetValue<string>("Settings:Jwt:SingingKey");

            services.AddScoped<IAccessTokenGenerator>(config => new JwtTokenGenerator(expirationTimeMinutes , signingKey!));
            services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        }
        private static void AddDbContext ( IServiceCollection services , IConfiguration configuration )
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<MiCashDbContext>(config => config.UseNpgsql(connectionString));
        }

        private static void AddRedis (
            this IServiceCollection services ,
            IConfiguration configuration )
        {

            var isInMemoryTest =
                configuration.GetValue<bool>("InMemoryTest");

            if ( isInMemoryTest ) {
                return;
            }
            var connectionString =
                configuration["Redis:ConnectionString"];

            if ( string.IsNullOrWhiteSpace(connectionString) ) {
                throw new InvalidOperationException(
                    "Redis connection string was not configured.");
            }

            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(connectionString));

            services.AddScoped<IRedisCacheService ,
                RedisCacheService>();
        }

        private static void AddPayments(IServiceCollection services , IConfiguration configuration )
        {
            services.AddHttpClient<IPaymentGateway , PagarMeService>()
                .AddMiCashResilience();

            services.Configure<PagarMeSettings>(
                configuration.GetSection("PagarMe"));

            services.Configure<WebhookSettings>(
                configuration.GetSection("WebhookSettings"));
        }

        private static void AddIA(IServiceCollection services, IConfiguration configuration )
        {
            services.Configure<GeminiSettings>(
                configuration.GetSection("Gemini"));

            services.AddHttpClient<IAIService , GeminiAIService>(client => {
                client.BaseAddress = new Uri(
                    "https://generativelanguage.googleapis.com/v1beta/");

                client.Timeout = AIHttpPolicies.TotalTimeout;
            }).AddAIResilience();
        }

        private static void AddPushNotifications ( IServiceCollection services , IConfiguration configuration )
        {
            services.Configure<ExpoPushSettings>(
                configuration.GetSection(ExpoPushSettings.SECTION));

            var settings = configuration.GetSection(ExpoPushSettings.SECTION).Get<ExpoPushSettings>() ?? new ExpoPushSettings();

            // Circuit breaker only: no HTTP retry, a resent "send" could duplicate notifications
            // (temporary failures are retried by the dispatch job).
            services.AddHttpClient<IExpoPushClient , ExpoPushClient>(client => {
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All
            })
            .AddPolicyHandler(HttpCircuitBreakerPolicy.Create());
        }

        private static void AddEmail ( IServiceCollection services , IConfiguration configuration )
        {
            // Validated on first use (first e-mail), not at startup: the API keeps running in an
            // environment without Resend configured, and only sending e-mails fails.
            services.AddOptions<ResendOptions>()
                .Bind(configuration.GetSection(ResendOptions.SECTION));

            services.AddSingleton<IValidateOptions<ResendOptions> , ResendOptionsValidator>();

            // The SDK options come from ours, so the key is read from a single place.
            services.AddOptions<ResendClientOptions>()
                .Configure<IOptions<ResendOptions>>(( client , resend ) => {
                    client.ApiToken = resend.Value.ApiKey;
                    client.ThrowExceptions = true;
                });

            // Circuit breaker only: no HTTP retry, a resent request could duplicate the e-mail
            // (the caller decides, using EmailServiceException.IsTransient and an idempotency key).
            services.AddHttpClient<IResend , ResendClient>(client => {
                client.Timeout = EMAIL_TIMEOUT;
            })
            .AddPolicyHandler(HttpCircuitBreakerPolicy.Create());

            services.AddScoped<IEmailService , ResendEmailService>();

            // Templates (embedded in this assembly) and their links.
            services.AddOptions<EmailTemplateOptions>()
                .Bind(configuration.GetSection(EmailTemplateOptions.SECTION));

            services.AddSingleton<IValidateOptions<EmailTemplateOptions> , EmailTemplateOptionsValidator>();
            services.AddSingleton<IEmailTemplateRenderer , EmailTemplateRenderer>();
        }

        private static readonly TimeSpan EMAIL_TIMEOUT = TimeSpan.FromSeconds(15);

    }
}
