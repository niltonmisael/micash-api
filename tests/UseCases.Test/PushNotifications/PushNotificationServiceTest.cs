using CommonTestUtilities.Jobs;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Time;
using FluentAssertions;
using MiCash.Application.Services.PushNotifications;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Domain.Services.PushNotifications;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;

namespace UseCases.Test.PushNotifications
{
    public class PushNotificationServiceTest
    {
        private const long USER_ID = 10;

        private readonly InMemoryPushRepository _repository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly FakeBackgroundJobService _jobs = new();
        private readonly MutableTimeProvider _time = new(new DateTimeOffset(2026 , 11 , 2 , 12 , 0 , 0 , TimeSpan.Zero));

        private PushNotificationService Service () => new(
            _repository , _repository , _repository , _unitOfWork.Object , _jobs , _time ,
            NullLogger<PushNotificationService>.Instance);

        [Fact]
        public async Task Creates_One_Pending_Delivery_Per_Active_Device_And_Schedules_Send ()
        {
            var phone = _repository.AddDevice(USER_ID , "ExponentPushToken[phone]");
            var tablet = _repository.AddDevice(USER_ID , "ExponentPushToken[tablet]");
            _repository.AddDevice(USER_ID , "ExponentPushToken[old]" , isActive: false);
            _repository.AddDevice(USER_ID + 1 , "ExponentPushToken[someone-else]");

            var result = await Service().EnqueueAsync(USER_ID , new PushMessage("test" , "Título" , "Corpo" , "/goals?tab=limits") , CancellationToken.None);

            result.Should().NotBeNull();
            result!.Devices.Should().Be(2);

            var notification = _repository.Notifications.Should().ContainSingle().Subject;
            notification.Deliveries.Select(d => d.DeviceId).Should().BeEquivalentTo([phone.Id , tablet.Id]);
            notification.Deliveries.Should().OnlyContain(d => d.Status == PushDeliveryStatus.Pending && d.NextAttemptAt == _time.UtcNow);

            var data = JsonSerializer.Deserialize<Dictionary<string , string>>(notification.Data)!;
            data.Should().Contain("url" , "/goals?tab=limits").And.Contain("type" , "test");

            _jobs.EnqueuedPushNotifications.Should().ContainSingle().Which.Should().Be(notification.Id);
        }

        [Fact]
        public async Task Without_Active_Devices_It_Is_Recorded_For_The_Center_But_Not_Sent ()
        {
            _repository.AddDevice(USER_ID , "ExponentPushToken[old]" , isActive: false);

            var result = await Service().EnqueueAsync(USER_ID , new PushMessage("test" , "T" , "B") , CancellationToken.None);

            result.Should().NotBeNull();
            result!.Devices.Should().Be(0);

            var notification = _repository.Notifications.Should().ContainSingle().Subject;
            notification.Id.Should().Be(result.NotificationId);
            notification.ReadAt.Should().BeNull();
            notification.Deliveries.Should().BeEmpty();
            _jobs.EnqueuedPushNotifications.Should().BeEmpty("there is no device to send to");
        }

        [Fact]
        public async Task Same_Idempotency_Key_Creates_A_Single_Notification ()
        {
            _repository.AddDevice(USER_ID , "ExponentPushToken[phone]");
            var message = new PushMessage("bill-due" , "Conta vence amanhã" , "Luz: R$ 150,00" , "/transactions" , "bill-due:42:2026-11");

            var first = await Service().EnqueueAsync(USER_ID , message , CancellationToken.None);
            var second = await Service().EnqueueAsync(USER_ID , message , CancellationToken.None);

            first.Should().NotBeNull();
            second.Should().BeNull();
            _repository.Notifications.Should().ContainSingle();
        }

        [Fact]
        public async Task Concurrent_Duplicate_Idempotency_Key_Is_Ignored ()
        {
            _repository.AddDevice(USER_ID , "ExponentPushToken[phone]");
            _unitOfWork.Setup(u => u.Commit(It.IsAny<CancellationToken>())).ThrowsAsync(new ConflictException());

            var result = await Service().EnqueueAsync(USER_ID , new PushMessage("x" , "T" , "B" , IdempotencyKey: "k") , CancellationToken.None);

            result.Should().BeNull();
            _jobs.EnqueuedPushNotifications.Should().BeEmpty();
        }

        [Theory]
        [InlineData("https://evil.example")]
        [InlineData("//evil.example")]
        [InlineData("/not-a-route")]
        public async Task External_Or_Unknown_Route_Is_Rejected ( string route )
        {
            _repository.AddDevice(USER_ID , "ExponentPushToken[phone]");

            var act = () => Service().EnqueueAsync(USER_ID , new PushMessage("test" , "T" , "B" , route) , CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
            _repository.Notifications.Should().BeEmpty();
        }

        [Fact]
        public async Task Hangfire_Unavailable_Does_Not_Lose_The_Notification ()
        {
            _repository.AddDevice(USER_ID , "ExponentPushToken[phone]");
            _jobs.ThrowOnEnqueue = true;

            var result = await Service().EnqueueAsync(USER_ID , new PushMessage("test" , "T" , "B") , CancellationToken.None);

            // Saved as Pending: the recurring dispatch job picks it up.
            result.Should().NotBeNull();
            _repository.Deliveries.Should().ContainSingle().Which.Status.Should().Be(PushDeliveryStatus.Pending);
        }
    }
}
