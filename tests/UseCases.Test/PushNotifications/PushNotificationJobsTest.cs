using CommonTestUtilities.Repositories;
using CommonTestUtilities.Time;
using FluentAssertions;
using MiCash.Application.Jobs.PushNotifications;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Services.PushNotifications;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UseCases.Test.PushNotifications
{
    public class PushNotificationJobsTest
    {
        private readonly InMemoryPushRepository _repository = new();
        private readonly FakeExpoPushClient _expo = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly MutableTimeProvider _time = new(new DateTimeOffset(2026 , 11 , 2 , 12 , 0 , 0 , TimeSpan.Zero));

        private Task Dispatch ( long? notificationId = null ) =>
            new PushNotificationDispatchJob(_repository , _expo , _unitOfWork.Object , _time , NullLogger<PushNotificationDispatchJob>.Instance)
                .ExecuteAsync(notificationId , CancellationToken.None);

        private Task CheckReceipts () =>
            new PushReceiptCheckJob(_repository , _expo , _unitOfWork.Object , _time , NullLogger<PushReceiptCheckJob>.Instance)
                .ExecuteAsync(CancellationToken.None);

        // ---------- dispatch ----------

        [Fact]
        public async Task Sends_Payload_And_Stores_Ticket ()
        {
            var device = _repository.AddDevice(1 , "ExponentPushToken[phone]");
            var delivery = _repository.AddDelivery(device , _time.UtcNow);

            await Dispatch();

            var message = _expo.SendCalls.Should().ContainSingle().Subject.Should().ContainSingle().Subject;
            message.To.Should().Be("ExponentPushToken[phone]");
            message.Title.Should().Be("Título");
            message.Body.Should().Be("Corpo");
            message.Data.Should().Contain("url" , "/home");
            message.Data.Should().Contain("notificationId" , delivery.NotificationId.ToString(), "the app marks the tapped notification as read");

            delivery.Status.Should().Be(PushDeliveryStatus.SentToExpo);
            delivery.ExpoTicketId.Should().Be("ticket-1");
            delivery.SentAt.Should().Be(_time.UtcNow);
            delivery.Attempts.Should().Be(1);
            device.LastUsedAt.Should().Be(_time.UtcNow);
        }

        [Fact]
        public async Task Rerunning_The_Job_Never_Sends_Twice ()
        {
            var device = _repository.AddDevice(1 , "ExponentPushToken[phone]");
            _repository.AddDelivery(device , _time.UtcNow);

            await Dispatch();
            await Dispatch();
            await Dispatch(_repository.Notifications[0].Id);

            _expo.SendCalls.Should().ContainSingle();
        }

        [Fact]
        public async Task Delivery_Is_Claimed_Before_Calling_Expo ()
        {
            var device = _repository.AddDevice(1 , "ExponentPushToken[phone]");
            var delivery = _repository.AddDelivery(device , _time.UtcNow);
            PushDeliveryStatus? statusWhenSending = null;
            var commitsBeforeSend = 0;
            var commits = 0;
            _unitOfWork.Setup(u => u.Commit(It.IsAny<CancellationToken>())).Callback(() => commits++).Returns(Task.CompletedTask);

            _expo.OnSend = messages => {
                statusWhenSending = delivery.Status;
                commitsBeforeSend = commits;
                return [new ExpoPushTicket(true , "t" , null , null)];
            };

            await Dispatch();

            statusWhenSending.Should().Be(PushDeliveryStatus.Sending);
            commitsBeforeSend.Should().Be(1);
        }

        [Fact]
        public async Task Claim_Conflict_Means_Another_Execution_Sends_It ()
        {
            var device = _repository.AddDevice(1 , "ExponentPushToken[phone]");
            _repository.AddDelivery(device , _time.UtcNow);
            _unitOfWork.Setup(u => u.Commit(It.IsAny<CancellationToken>())).ThrowsAsync(new ConflictException());

            await Dispatch();

            _expo.SendCalls.Should().BeEmpty();
        }

        [Fact]
        public async Task Batches_Respect_Expo_Limit_Of_100 ()
        {
            for ( var i = 0; i < 250; i++ ) {
                _repository.AddDelivery(_repository.AddDevice(1 , $"ExponentPushToken[d{i}]") , _time.UtcNow);
            }

            await Dispatch();

            _expo.SendCalls.Select(c => c.Count).Should().Equal(100 , 100 , 50);
            _repository.Deliveries.Should().OnlyContain(d => d.Status == PushDeliveryStatus.SentToExpo);
        }

        [Fact]
        public async Task Partial_Response_Device_Not_Registered_Deactivates_Only_That_Device ()
        {
            var good = _repository.AddDevice(1 , "ExponentPushToken[good]");
            var gone = _repository.AddDevice(1 , "ExponentPushToken[gone]");
            var okDelivery = _repository.AddDelivery(good , _time.UtcNow);
            var goneDelivery = _repository.AddDelivery(gone , _time.UtcNow);

            _expo.OnSend = _ => [
                new ExpoPushTicket(true , "t1" , null , null) ,
                new ExpoPushTicket(false , null , ExpoPushErrors.DEVICE_NOT_REGISTERED , "\"ExponentPushToken[gone]\" is not a registered push notification recipient")
            ];

            await Dispatch();

            okDelivery.Status.Should().Be(PushDeliveryStatus.SentToExpo);
            goneDelivery.Status.Should().Be(PushDeliveryStatus.Failed);
            goneDelivery.ErrorCode.Should().Be(ExpoPushErrors.DEVICE_NOT_REGISTERED);
            goneDelivery.ErrorMessage.Should().NotContain("[gone]");
            gone.IsActive.Should().BeFalse();
            good.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Other_Ticket_Errors_Fail_Without_Deactivating ()
        {
            var device = _repository.AddDevice(1 , "ExponentPushToken[phone]");
            var delivery = _repository.AddDelivery(device , _time.UtcNow);
            _expo.OnSend = _ => [new ExpoPushTicket(false , null , "MessageTooBig" , "too big")];

            await Dispatch();

            delivery.Status.Should().Be(PushDeliveryStatus.Failed);
            delivery.ErrorCode.Should().Be("MessageTooBig");
            device.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Temporary_Failure_Is_Retried_With_Backoff_Until_Max_Attempts ()
        {
            var device = _repository.AddDevice(1 , "ExponentPushToken[phone]");
            var delivery = _repository.AddDelivery(device , _time.UtcNow);
            _expo.OnSend = _ => throw new PushServiceException(PushFailureKind.Retryable , "HTTP 503");

            await Dispatch();

            delivery.Status.Should().Be(PushDeliveryStatus.Pending);
            delivery.Attempts.Should().Be(1);
            delivery.NextAttemptAt.Should().Be(_time.UtcNow + TimeSpan.FromMinutes(1));
            device.IsActive.Should().BeTrue();

            // Not due yet: nothing is sent.
            await Dispatch();
            _expo.SendCalls.Should().HaveCount(1);

            _time.Advance(TimeSpan.FromMinutes(1));
            await Dispatch();
            delivery.Attempts.Should().Be(2);
            delivery.NextAttemptAt.Should().Be(_time.UtcNow + TimeSpan.FromMinutes(2));

            _time.Advance(TimeSpan.FromMinutes(2));
            await Dispatch();
            delivery.Attempts.Should().Be(PushNotificationDispatchJob.MAX_ATTEMPTS);
            delivery.Status.Should().Be(PushDeliveryStatus.Failed);
            delivery.ErrorCode.Should().Be("ExpoUnavailable");
            device.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Message_Rate_Exceeded_Is_Temporary ()
        {
            var delivery = _repository.AddDelivery(_repository.AddDevice(1 , "ExponentPushToken[phone]") , _time.UtcNow);
            _expo.OnSend = _ => [new ExpoPushTicket(false , null , ExpoPushErrors.MESSAGE_RATE_EXCEEDED , "slow down")];

            await Dispatch();

            delivery.Status.Should().Be(PushDeliveryStatus.Pending);
            delivery.Device.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Uncertain_Failure_Is_Never_Resent ()
        {
            var delivery = _repository.AddDelivery(_repository.AddDevice(1 , "ExponentPushToken[phone]") , _time.UtcNow);
            _expo.OnSend = _ => throw new PushServiceException(PushFailureKind.Uncertain , "timeout");

            await Dispatch();
            _time.Advance(TimeSpan.FromHours(1));
            await Dispatch();

            delivery.Status.Should().Be(PushDeliveryStatus.Unconfirmed);
            _expo.SendCalls.Should().ContainSingle();
        }

        [Fact]
        public async Task Rejected_Request_Fails_Without_Retry ()
        {
            var delivery = _repository.AddDelivery(_repository.AddDevice(1 , "ExponentPushToken[phone]") , _time.UtcNow);
            _expo.OnSend = _ => throw new PushServiceException(PushFailureKind.Rejected , "HTTP 400");

            await Dispatch();

            delivery.Status.Should().Be(PushDeliveryStatus.Failed);
            delivery.ErrorCode.Should().Be("ExpoRejected");
        }

        [Fact]
        public async Task Inactive_Device_Is_Not_Sent ()
        {
            var delivery = _repository.AddDelivery(_repository.AddDevice(1 , "ExponentPushToken[phone]" , isActive: false) , _time.UtcNow);

            await Dispatch();

            _expo.SendCalls.Should().BeEmpty();
            delivery.Status.Should().Be(PushDeliveryStatus.Failed);
            delivery.ErrorCode.Should().Be("DeviceInactive");
        }

        // ---------- receipts ----------

        private SentDelivery Sent ( string ticketId , TimeSpan ago , string token = "ExponentPushToken[phone]" )
        {
            var device = _repository.AddDevice(1 , token);
            var delivery = _repository.AddDelivery(device , _time.UtcNow - ago);
            delivery.Status = PushDeliveryStatus.SentToExpo;
            delivery.ExpoTicketId = ticketId;
            delivery.SentAt = _time.UtcNow - ago;

            return new SentDelivery(device , delivery);
        }

        private sealed record SentDelivery ( MiCash.Domain.Entities.PushDevice Device , MiCash.Domain.Entities.PushNotificationDelivery Delivery );

        [Fact]
        public async Task Receipts_Are_Only_Read_15_Minutes_After_Sending ()
        {
            Sent("recent" , TimeSpan.FromMinutes(5));

            await CheckReceipts();

            _expo.ReceiptCalls.Should().BeEmpty();
        }

        [Fact]
        public async Task Receipts_Update_Status_And_Deactivate_Unregistered_Devices ()
        {
            var ok = Sent("ok" , TimeSpan.FromMinutes(20) , "ExponentPushToken[a]");
            var gone = Sent("gone" , TimeSpan.FromMinutes(20) , "ExponentPushToken[b]");
            var tooBig = Sent("big" , TimeSpan.FromMinutes(20) , "ExponentPushToken[c]");
            var waiting = Sent("waiting" , TimeSpan.FromMinutes(20) , "ExponentPushToken[d]");
            var expired = Sent("expired" , TimeSpan.FromHours(25) , "ExponentPushToken[e]");

            _expo.OnReceipts = _ => new Dictionary<string , ExpoPushReceipt>
            {
                ["ok"] = new(true , null , null) ,
                ["gone"] = new(false , ExpoPushErrors.DEVICE_NOT_REGISTERED , "unregistered") ,
                ["big"] = new(false , "MessageTooBig" , "too big")
            };

            await CheckReceipts();

            _expo.ReceiptCalls.Should().ContainSingle().Which.Should().BeEquivalentTo(["ok" , "gone" , "big" , "waiting" , "expired"]);

            ok.Delivery.Status.Should().Be(PushDeliveryStatus.Delivered);

            gone.Delivery.Status.Should().Be(PushDeliveryStatus.Failed);
            gone.Device.IsActive.Should().BeFalse();

            tooBig.Delivery.Status.Should().Be(PushDeliveryStatus.Failed);
            tooBig.Device.IsActive.Should().BeTrue();

            waiting.Delivery.Status.Should().Be(PushDeliveryStatus.SentToExpo);
            waiting.Delivery.ReceiptCheckedAt.Should().Be(_time.UtcNow);

            expired.Delivery.Status.Should().Be(PushDeliveryStatus.Unconfirmed);
            expired.Device.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Receipt_Request_Failure_Changes_Nothing ()
        {
            var sent = Sent("t" , TimeSpan.FromMinutes(20));
            _expo.OnReceipts = _ => throw new PushServiceException(PushFailureKind.Retryable , "HTTP 503");

            await CheckReceipts();

            sent.Delivery.Status.Should().Be(PushDeliveryStatus.SentToExpo);
            sent.Delivery.ReceiptCheckedAt.Should().BeNull();
            sent.Device.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Delivery_Stuck_In_Sending_Becomes_Unconfirmed_And_Is_Not_Resent ()
        {
            var delivery = _repository.AddDelivery(_repository.AddDevice(1 , "ExponentPushToken[phone]") , _time.UtcNow);
            delivery.Status = PushDeliveryStatus.Sending;
            delivery.ClaimedAt = _time.UtcNow;

            _time.Advance(TimeSpan.FromMinutes(11));
            await CheckReceipts();
            await Dispatch();

            delivery.Status.Should().Be(PushDeliveryStatus.Unconfirmed);
            delivery.ErrorCode.Should().Be("SendInterrupted");
            _expo.SendCalls.Should().BeEmpty();
        }
    }
}
