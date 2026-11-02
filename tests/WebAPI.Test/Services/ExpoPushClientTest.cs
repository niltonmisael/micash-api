using FluentAssertions;
using MiCash.Domain.Services.PushNotifications;
using MiCash.Infrastructure.Services.PushNotifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace WebAPI.Test.Services
{
    public class ExpoPushClientTest
    {
        private const string TOKEN = "ExponentPushToken[secretTOKENvalue123456]";

        private static readonly ExpoPushMessage Message = new(
            TOKEN , "Nova movimentação" , "Uma nova despesa foi registrada." ,
            new Dictionary<string , string> { ["url"] = "/transactions" , ["type"] = "test" });

        [Fact]
        public async Task Send_Builds_Expo_Payload ()
        {
            var handler = new StubHandler(HttpStatusCode.OK , """{"data":[{"status":"ok","id":"ticket-1"}]}""");

            var tickets = await Client(handler).SendAsync([Message] , CancellationToken.None);

            tickets.Should().ContainSingle().Which.Should().Be(new ExpoPushTicket(true , "ticket-1" , null , null));

            handler.Request!.Method.Should().Be(HttpMethod.Post);
            handler.Request.RequestUri!.ToString().Should().Be("https://exp.host/--/api/v2/push/send");
            handler.Request.Headers.Authorization.Should().BeNull("no access token configured");

            var item = JsonDocument.Parse(handler.Body!).RootElement.EnumerateArray().Single();
            item.GetProperty("to").GetString().Should().Be(TOKEN);
            item.GetProperty("title").GetString().Should().Be("Nova movimentação");
            item.GetProperty("body").GetString().Should().Be("Uma nova despesa foi registrada.");
            item.GetProperty("sound").GetString().Should().Be("default");
            item.GetProperty("priority").GetString().Should().Be("high");
            item.GetProperty("channelId").GetString().Should().Be("default");
            item.GetProperty("data").GetProperty("url").GetString().Should().Be("/transactions");
        }

        [Fact]
        public async Task Access_Token_Is_Sent_When_Configured ()
        {
            var handler = new StubHandler(HttpStatusCode.OK , """{"data":[{"status":"ok","id":"t"}]}""");

            await Client(handler , new ExpoPushSettings { AccessToken = "expo-access" }).SendAsync([Message] , CancellationToken.None);

            handler.Request!.Headers.Authorization!.Scheme.Should().Be("Bearer");
            handler.Request.Headers.Authorization.Parameter.Should().Be("expo-access");
        }

        [Fact]
        public async Task Ticket_Errors_Are_Returned_Per_Message_With_Token_Masked ()
        {
            var handler = new StubHandler(HttpStatusCode.OK , $$$"""
            {"data":[
              {"status":"ok","id":"t1"},
              {"status":"error","message":"\"{{{TOKEN}}}\" is not a registered push notification recipient","details":{"error":"DeviceNotRegistered","expoPushToken":"{{{TOKEN}}}"}}
            ]}
            """);
            var logger = new CapturingLogger();

            var tickets = await Client(handler , logger: logger).SendAsync([Message , Message] , CancellationToken.None);

            tickets[0].IsOk.Should().BeTrue();
            tickets[1].IsOk.Should().BeFalse();
            tickets[1].ErrorCode.Should().Be(ExpoPushErrors.DEVICE_NOT_REGISTERED);
            tickets[1].ErrorMessage.Should().NotContain("secretTOKEN").And.Contain("…123456");
        }

        [Theory]
        [InlineData(HttpStatusCode.TooManyRequests , PushFailureKind.Retryable)]
        [InlineData(HttpStatusCode.InternalServerError , PushFailureKind.Retryable)]
        [InlineData(HttpStatusCode.BadGateway , PushFailureKind.Retryable)]
        [InlineData(HttpStatusCode.BadRequest , PushFailureKind.Rejected)]
        [InlineData(HttpStatusCode.Unauthorized , PushFailureKind.Rejected)]
        public async Task Http_Errors_Are_Classified ( HttpStatusCode status , PushFailureKind expected )
        {
            var logger = new CapturingLogger();
            var handler = new StubHandler(status , $$"""{"errors":[{"code":"X","message":"bad {{TOKEN}}"}]}""");

            var act = () => Client(handler , logger: logger).SendAsync([Message] , CancellationToken.None);

            (await act.Should().ThrowAsync<PushServiceException>()).Which.Kind.Should().Be(expected);
            logger.Messages.Should().NotContain(m => m.Contains("secretTOKEN"));
        }

        [Fact]
        public async Task Ticket_Count_Mismatch_Is_Uncertain ()
        {
            var handler = new StubHandler(HttpStatusCode.OK , """{"data":[{"status":"ok","id":"t1"}]}""");

            var act = () => Client(handler).SendAsync([Message , Message] , CancellationToken.None);

            (await act.Should().ThrowAsync<PushServiceException>()).Which.Kind.Should().Be(PushFailureKind.Uncertain);
        }

        [Fact]
        public async Task Timeout_After_Sending_Is_Uncertain ()
        {
            var httpClient = new HttpClient(new SlowHandler())
            {
                BaseAddress = new Uri("https://exp.host") ,
                Timeout = TimeSpan.FromMilliseconds(100)
            };

            var client = new ExpoPushClient(httpClient , Options.Create(new ExpoPushSettings()) , new CapturingLogger());

            var act = () => client.SendAsync([Message] , CancellationToken.None);

            (await act.Should().ThrowAsync<PushServiceException>()).Which.Kind.Should().Be(PushFailureKind.Uncertain);
        }

        [Fact]
        public async Task Connection_Failure_Is_Retryable ()
        {
            var handler = new ThrowingHandler(new HttpRequestException(HttpRequestError.ConnectionError , "refused"));

            var act = () => Client(handler).SendAsync([Message] , CancellationToken.None);

            (await act.Should().ThrowAsync<PushServiceException>()).Which.Kind.Should().Be(PushFailureKind.Retryable);
        }

        [Fact]
        public async Task Caller_Cancellation_Is_Not_A_Push_Failure ()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = () => Client(new SlowHandler()).SendAsync([Message] , cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task More_Than_100_Messages_Is_Refused_Before_Sending ()
        {
            var handler = new StubHandler(HttpStatusCode.OK , "{}");

            var act = () => Client(handler).SendAsync(Enumerable.Repeat(Message , 101).ToList() , CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
            handler.Request.Should().BeNull();
        }

        [Fact]
        public async Task Receipts_Are_Parsed ()
        {
            var handler = new StubHandler(HttpStatusCode.OK , """
            {"data":{
              "a":{"status":"ok"},
              "b":{"status":"error","message":"The device cannot receive push notifications anymore","details":{"error":"DeviceNotRegistered"}}
            }}
            """);

            var receipts = await Client(handler).GetReceiptsAsync(["a" , "b" , "c"] , CancellationToken.None);

            handler.Request!.RequestUri!.ToString().Should().Be("https://exp.host/--/api/v2/push/getReceipts");
            JsonDocument.Parse(handler.Body!).RootElement.GetProperty("ids").GetArrayLength().Should().Be(3);

            receipts["a"].IsOk.Should().BeTrue();
            receipts["b"].ErrorCode.Should().Be(ExpoPushErrors.DEVICE_NOT_REGISTERED);
            receipts.Should().NotContainKey("c");
        }

        [Fact]
        public async Task Receipt_Errors_Are_Retryable ()
        {
            var act = () => Client(new StubHandler(HttpStatusCode.ServiceUnavailable , "")).GetReceiptsAsync(["a"] , CancellationToken.None);

            (await act.Should().ThrowAsync<PushServiceException>()).Which.Kind.Should().Be(PushFailureKind.Retryable);
        }

        private static ExpoPushClient Client ( HttpMessageHandler handler , ExpoPushSettings? settings = null , ILogger<ExpoPushClient>? logger = null )
        {
            settings ??= new ExpoPushSettings();

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri(settings.BaseUrl) };

            return new ExpoPushClient(httpClient , Options.Create(settings) , logger ?? new CapturingLogger());
        }

        private sealed class StubHandler ( HttpStatusCode status , string body ) : HttpMessageHandler
        {
            public HttpRequestMessage? Request { get; private set; }

            public string? Body { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync ( HttpRequestMessage request , CancellationToken cancellationToken )
            {
                Request = request;
                Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

                return new HttpResponseMessage(status) { Content = new StringContent(body , System.Text.Encoding.UTF8 , "application/json") };
            }
        }

        private sealed class SlowHandler : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync ( HttpRequestMessage request , CancellationToken cancellationToken )
            {
                await Task.Delay(TimeSpan.FromSeconds(30) , cancellationToken);

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        private sealed class ThrowingHandler ( System.Exception exception ) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync ( HttpRequestMessage request , CancellationToken cancellationToken ) =>
                throw exception;
        }

        private sealed class CapturingLogger : ILogger<ExpoPushClient>
        {
            public List<string> Messages { get; } = [];

            public IDisposable? BeginScope<TState> ( TState state ) where TState : notnull => null;

            public bool IsEnabled ( LogLevel logLevel ) => true;

            public void Log<TState> ( LogLevel logLevel , EventId eventId , TState state , System.Exception? exception , Func<TState , System.Exception? , string> formatter ) =>
                Messages.Add(formatter(state , exception) + " " + exception?.Message);
        }
    }
}
