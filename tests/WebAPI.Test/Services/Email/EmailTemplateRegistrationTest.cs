using FluentAssertions;
using MiCash.Domain.Services.Email;
using MiCash.Infrastructure.Services.Email;
using Microsoft.Extensions.DependencyInjection;

namespace WebAPI.Test.Services.Email
{
    /// <summary>The renderer as the API builds it (real DI + appsettings.json links).</summary>
    public class EmailTemplateRegistrationTest : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EmailTemplateRegistrationTest ( CustomWebApplicationFactory factory ) => _factory = factory;

        [Fact]
        public void Renderer_Is_Registered_And_Uses_The_Configured_Links ()
        {
            using var scope = _factory.Services.CreateScope();

            var renderer = scope.ServiceProvider.GetRequiredService<IEmailTemplateRenderer>();
            renderer.Should().BeOfType<EmailTemplateRenderer>();

            var rendered = renderer.Render(new WelcomeEmail("Maria"));

            rendered.Subject.Should().Be("Bem-vindo ao MiCash");
            rendered.HtmlBody.Should().Contain("href=\"https://micash.com.br/ajuda\"");
        }
    }
}
