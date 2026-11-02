using FluentAssertions;
using MiCash.Domain.Services.Email;
using MiCash.Infrastructure;
using MiCash.Infrastructure.Services.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Resend;
using System.Reflection;

namespace WebAPI.Test.Services.Email
{
    public class ResendConfigurationTest
    {
        private static readonly Dictionary<string , string?> ValidSettings = new()
        {
            ["Resend:ApiKey"] = "re_test_key" ,
            ["Resend:FromEmail"] = "notificacoes@mail.micash.com.br" ,
            ["Resend:FromName"] = "MiCash"
        };

        [Fact]
        public void Section_Is_Bound_To_ResendOptions ()
        {
            using var provider = BuildProvider(ValidSettings);

            var options = provider.GetRequiredService<IOptions<ResendOptions>>().Value;

            options.ApiKey.Should().Be("re_test_key");
            options.FromEmail.Should().Be("notificacoes@mail.micash.com.br");
            options.FromName.Should().Be("MiCash");
        }

        [Fact]
        public void Sdk_Options_Use_The_Configured_Key_And_Throw_On_Failure ()
        {
            using var provider = BuildProvider(ValidSettings);

            var options = provider.GetRequiredService<IOptions<ResendClientOptions>>().Value;

            options.ApiToken.Should().Be("re_test_key");
            options.ThrowExceptions.Should().BeTrue();
        }

        [Fact]
        public void Email_Service_Is_Registered_With_Resend ()
        {
            using var provider = BuildProvider(ValidSettings);
            using var scope = provider.CreateScope();

            scope.ServiceProvider.GetRequiredService<IEmailService>().Should().BeOfType<ResendEmailService>();
            scope.ServiceProvider.GetRequiredService<IResend>().Should().BeOfType<ResendClient>();
        }

        [Fact]
        public void Missing_Settings_Fail_When_The_Service_Is_Used_Without_Exposing_Values ()
        {
            using var provider = BuildProvider(new Dictionary<string , string?>
            {
                ["Resend:ApiKey"] = "" ,
                ["Resend:FromEmail"] = "" ,
                ["Resend:FromName"] = ""
            });
            using var scope = provider.CreateScope();

            var act = () => scope.ServiceProvider.GetRequiredService<IEmailService>();

            act.Should().Throw<OptionsValidationException>()
                .Which.Failures.Should().BeEquivalentTo(
                    "Resend:ApiKey is required." ,
                    "Resend:FromEmail is required." ,
                    "Resend:FromName is required.");
        }

        [Theory]
        [InlineData("" , "notificacoes@mail.micash.com.br" , "MiCash" , "Resend:ApiKey is required.")]
        [InlineData("re_key" , "" , "MiCash" , "Resend:FromEmail is required.")]
        [InlineData("re_key" , "notificacoes" , "MiCash" , "Resend:FromEmail is not a valid e-mail address.")]
        [InlineData("re_key" , "MiCash <notificacoes@mail.micash.com.br>" , "MiCash" , "Resend:FromEmail is not a valid e-mail address.")]
        [InlineData("re_key" , "notificacoes@mail.micash.com.br" , " " , "Resend:FromName is required.")]
        public void Validator_Reports_Each_Invalid_Setting ( string apiKey , string fromEmail , string fromName , string expected )
        {
            var result = new ResendOptionsValidator().Validate(null , new ResendOptions
            {
                ApiKey = apiKey ,
                FromEmail = fromEmail ,
                FromName = fromName
            });

            result.Failed.Should().BeTrue();
            result.Failures.Should().ContainSingle().Which.Should().Be(expected);
        }

        [Fact]
        public void Validator_Accepts_Valid_Settings ()
        {
            var result = new ResendOptionsValidator().Validate(null , new ResendOptions
            {
                ApiKey = "re_key" ,
                FromEmail = "notificacoes@mail.micash.com.br" ,
                FromName = "MiCash"
            });

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public void Api_Project_Has_User_Secrets_So_Development_Loads_Them ()
        {
            // WebApplication.CreateBuilder only loads user-secrets (Development) when this attribute exists.
            typeof(Program).Assembly.GetCustomAttribute<UserSecretsIdAttribute>()
                .Should().NotBeNull()
                .And.Subject.As<UserSecretsIdAttribute>().UserSecretsId.Should().NotBeNullOrWhiteSpace();
        }

        // The real registration (AddInfrastructure) with an in-memory configuration.
        private static ServiceProvider BuildProvider ( Dictionary<string , string?> settings )
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string , string?>(settings) { ["InMemoryTest"] = "true" })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(configuration);

            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        }
    }
}
