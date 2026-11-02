using FluentAssertions;
using MiCash.Application.Services.Email;
using MiCash.Domain.Services.Email;
using Moq;

namespace UseCases.Test.Email
{
    public class TransactionalEmailServiceTest
    {
        private readonly Mock<IEmailTemplateRenderer> _renderer = new();
        private readonly Mock<IEmailService> _emailService = new();

        [Fact]
        public async Task Renders_The_Model_And_Sends_The_Result ()
        {
            var model = new WelcomeEmail("Maria");
            _renderer.Setup(r => r.Render(model)).Returns(new RenderedEmail("Bem-vindo ao MiCash" , "<p>html</p>" , "texto"));

            EmailMessage? sent = null;
            _emailService.Setup(s => s.SendAsync(It.IsAny<EmailMessage>() , It.IsAny<CancellationToken>()))
                .Callback<EmailMessage , CancellationToken>(( message , _ ) => sent = message)
                .ReturnsAsync("email-id");

            var id = await Service().SendAsync("maria@gmail.com" , model , idempotencyKey: "welcome-1");

            id.Should().Be("email-id");
            sent.Should().Be(new EmailMessage("maria@gmail.com" , "Bem-vindo ao MiCash" , "<p>html</p>" , "texto" , "welcome-1"));
        }

        [Fact]
        public async Task Invalid_Model_Is_Not_Sent ()
        {
            _renderer.Setup(r => r.Render(It.IsAny<TransactionalEmail>())).Throws(new ArgumentException("Name is required."));

            var act = () => Service().SendAsync("maria@gmail.com" , new WelcomeEmail(""));

            await act.Should().ThrowAsync<ArgumentException>();
            _emailService.Invocations.Should().BeEmpty();
        }

        [Fact]
        public async Task Provider_Failure_Is_Propagated ()
        {
            _renderer.Setup(r => r.Render(It.IsAny<TransactionalEmail>())).Returns(new RenderedEmail("s" , "h" , "t"));
            _emailService.Setup(s => s.SendAsync(It.IsAny<EmailMessage>() , It.IsAny<CancellationToken>()))
                .ThrowsAsync(new EmailServiceException("RateLimitExceeded" , isTransient: true , "Failed"));

            var act = () => Service().SendAsync("maria@gmail.com" , new WelcomeEmail("Maria"));

            (await act.Should().ThrowAsync<EmailServiceException>()).Which.IsTransient.Should().BeTrue();
        }

        private TransactionalEmailService Service () => new(_renderer.Object , _emailService.Object);
    }
}
