using FluentAssertions;
using MiCash.Domain.Services.Email;
using MiCash.Infrastructure.Services.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Resend;
using System.Net;
using EmailMessage = MiCash.Domain.Services.Email.EmailMessage;

namespace WebAPI.Test.Services.Email
{
    public class ResendEmailServiceTest
    {
        private const string API_KEY = "re_secretAPIkey123";
        private const string RECIPIENT = "joao.silva@gmail.com";
        private const string SECRET_CODE = "CODE-987654";

        private static readonly ResendOptions Options = new()
        {
            ApiKey = API_KEY ,
            FromEmail = "notificacoes@mail.micash.com.br" ,
            FromName = "MiCash"
        };

        private readonly Mock<IResend> _resend = new();
        private readonly CapturingLogger _logger = new();
        private Resend.EmailMessage? _sent;
        private string? _sentIdempotencyKey;

        [Fact]
        public async Task Html_And_Text_Email_Is_Sent_From_The_Configured_Sender ()
        {
            var id = Guid.NewGuid();
            SetupSuccess(id);

            var result = await Service().SendAsync(new EmailMessage(
                RECIPIENT , "Seu código" , HtmlBody: $"<p>{SECRET_CODE}</p>" , TextBody: SECRET_CODE));

            result.Should().Be(id.ToString());

            var from = _sent!.From!;
            from.Email.Should().Be("notificacoes@mail.micash.com.br");
            from.DisplayName.Should().Be("MiCash");
            _sent.To!.Should().ContainSingle().Which.Email.Should().Be(RECIPIENT);
            _sent.Subject.Should().Be("Seu código");
            _sent.HtmlBody.Should().Be($"<p>{SECRET_CODE}</p>");
            _sent.TextBody.Should().Be(SECRET_CODE);
            _sentIdempotencyKey.Should().BeNull();
        }

        [Fact]
        public async Task Text_Only_Email_Is_Sent ()
        {
            SetupSuccess(Guid.NewGuid());

            await Service().SendAsync(new EmailMessage(RECIPIENT , "Aviso" , TextBody: "Somente texto"));

            _sent!.HtmlBody.Should().BeNull();
            _sent.TextBody.Should().Be("Somente texto");
        }

        [Fact]
        public async Task Idempotency_Key_Is_Forwarded ()
        {
            SetupSuccess(Guid.NewGuid());

            await Service().SendAsync(new EmailMessage(RECIPIENT , "Aviso" , TextBody: "x" , IdempotencyKey: "welcome-42"));

            _sentIdempotencyKey.Should().Be("welcome-42");
        }

        [Fact]
        public async Task Logs_Do_Not_Contain_Key_Body_Subject_Or_Full_Recipient ()
        {
            SetupSuccess(Guid.NewGuid());

            await Service().SendAsync(new EmailMessage(RECIPIENT , $"Código {SECRET_CODE}" , TextBody: SECRET_CODE));

            _logger.Messages.Should().NotBeEmpty();
            _logger.Messages.Should().AllSatisfy(m => {
                m.Should().NotContain(API_KEY);
                m.Should().NotContain(SECRET_CODE);
                m.Should().NotContain(RECIPIENT);
            });
            _logger.Messages.Should().Contain(m => m.Contains("j***@gmail.com"));
        }

        [Theory]
        [InlineData("" , "Assunto" , "texto" , null)]
        [InlineData("nao-e-email" , "Assunto" , "texto" , null)]
        [InlineData("a@b.com, c@d.com" , "Assunto" , "texto" , null)]
        [InlineData(RECIPIENT , " " , "texto" , null)]
        [InlineData(RECIPIENT , "Assunto" , null , " ")]
        public async Task Invalid_Message_Is_Refused_Before_Calling_Resend ( string to , string subject , string? text , string? html )
        {
            var act = () => Service().SendAsync(new EmailMessage(to , subject , HtmlBody: html , TextBody: text));

            await act.Should().ThrowAsync<ArgumentException>();
            _resend.Invocations.Should().BeEmpty();
        }

        [Theory]
        [InlineData(HttpStatusCode.UnprocessableEntity , ErrorType.ValidationError , false)]
        [InlineData(HttpStatusCode.Forbidden , ErrorType.InvalidFromAddress , false)]
        [InlineData(HttpStatusCode.Unauthorized , ErrorType.InvalidApiKey , false)]
        [InlineData(HttpStatusCode.TooManyRequests , ErrorType.RateLimitExceeded , true)]
        [InlineData(HttpStatusCode.TooManyRequests , ErrorType.DailyQuotaExceeded , true)]
        [InlineData(HttpStatusCode.InternalServerError , ErrorType.InternalServerError , true)]
        [InlineData(null , ErrorType.HttpSendFailed , true)]
        public async Task Provider_Errors_Become_EmailServiceException ( HttpStatusCode? status , ErrorType errorType , bool transient )
        {
            // Provider text echoing the recipient: must not reach the logs.
            var providerError = new ResendException(status , errorType , $"Invalid `to` field: {RECIPIENT}" , null!);

            _resend.Setup(r => r.EmailSendAsync(It.IsAny<Resend.EmailMessage>() , It.IsAny<CancellationToken>()))
                .ThrowsAsync(providerError);

            var act = () => Service().SendAsync(new EmailMessage(RECIPIENT , "Assunto" , TextBody: "x"));

            var exception = (await act.Should().ThrowAsync<EmailServiceException>()).Which;
            exception.ErrorCode.Should().Be(errorType.ToString());
            exception.IsTransient.Should().Be(transient);
            exception.InnerException.Should().BeSameAs(providerError);
            exception.Message.Should().NotContain(RECIPIENT);

            _logger.Messages.Should().ContainSingle(m => m.Contains(errorType.ToString()));
            _logger.Messages.Should().NotContain(m => m.Contains(RECIPIENT));
        }

        [Fact]
        public async Task Unsuccessful_Response_Without_Exception_Thrown_Is_Also_A_Failure ()
        {
            var providerError = new ResendException(HttpStatusCode.UnprocessableEntity , ErrorType.ValidationError , "invalid" , null!);

            _resend.Setup(r => r.EmailSendAsync(It.IsAny<Resend.EmailMessage>() , It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResendResponse<Guid>(providerError , null!));

            var act = () => Service().SendAsync(new EmailMessage(RECIPIENT , "Assunto" , TextBody: "x"));

            (await act.Should().ThrowAsync<EmailServiceException>()).Which.IsTransient.Should().BeFalse();
        }

        [Fact]
        public async Task Timeout_Is_Transient ()
        {
            _resend.Setup(r => r.EmailSendAsync(It.IsAny<Resend.EmailMessage>() , It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException("HttpClient.Timeout"));

            var act = () => Service().SendAsync(new EmailMessage(RECIPIENT , "Assunto" , TextBody: "x"));

            var exception = (await act.Should().ThrowAsync<EmailServiceException>()).Which;
            exception.ErrorCode.Should().Be("Timeout");
            exception.IsTransient.Should().BeTrue();
        }

        [Fact]
        public async Task Caller_Cancellation_Is_Not_An_Email_Failure ()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            _resend.Setup(r => r.EmailSendAsync(It.IsAny<Resend.EmailMessage>() , It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException());

            var act = () => Service().SendAsync(new EmailMessage(RECIPIENT , "Assunto" , TextBody: "x") , cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        private void SetupSuccess ( Guid id )
        {
            _resend.Setup(r => r.EmailSendAsync(It.IsAny<Resend.EmailMessage>() , It.IsAny<CancellationToken>()))
                .Callback<Resend.EmailMessage , CancellationToken>(( message , _ ) => _sent = message)
                .ReturnsAsync(new ResendResponse<Guid>(id , null!));

            _resend.Setup(r => r.EmailSendAsync(It.IsAny<string>() , It.IsAny<Resend.EmailMessage>() , It.IsAny<CancellationToken>()))
                .Callback<string , Resend.EmailMessage , CancellationToken>(( key , message , _ ) => {
                    _sentIdempotencyKey = key;
                    _sent = message;
                })
                .ReturnsAsync(new ResendResponse<Guid>(id , null!));
        }

        private ResendEmailService Service () =>
            new(_resend.Object , Microsoft.Extensions.Options.Options.Create(Options) , _logger);

        private sealed class CapturingLogger : ILogger<ResendEmailService>
        {
            public List<string> Messages { get; } = [];

            public IDisposable? BeginScope<TState> ( TState state ) where TState : notnull => null;

            public bool IsEnabled ( LogLevel logLevel ) => true;

            public void Log<TState> ( LogLevel logLevel , EventId eventId , TState state , System.Exception? exception , Func<TState , System.Exception? , string> formatter ) =>
                Messages.Add(formatter(state , exception));
        }
    }
}
