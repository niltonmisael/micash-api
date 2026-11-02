using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Time;
using FluentAssertions;
using MiCash.Application.UseCases.PushDevices;
using MiCash.Communication.Requests.PushDevices;
using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UseCases.Test.PushNotifications
{
    public class PushDeviceUseCasesTest
    {
        private const string TOKEN = "ExponentPushToken[aaaaaaaaaaaa111111]";
        private const string OTHER_TOKEN = "ExponentPushToken[bbbbbbbbbbbb222222]";

        private readonly User _user = UserBuilder.Build();
        private readonly InMemoryPushRepository _repository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly MutableTimeProvider _time = new(new DateTimeOffset(2026 , 11 , 2 , 12 , 0 , 0 , TimeSpan.Zero));

        [Fact]
        public async Task Register_New_Token_Belongs_To_Logged_User ()
        {
            await Register(Request(TOKEN , "ios"));

            var device = _repository.Devices.Should().ContainSingle().Subject;
            device.UserId.Should().Be(_user.Id);
            device.Platform.Should().Be(DevicePlatform.Ios);
            device.DeviceName.Should().Be("Pixel");
            device.IsActive.Should().BeTrue();
            _unitOfWork.Verify(u => u.Commit(It.IsAny<CancellationToken>()) , Times.Once);
        }

        [Fact]
        public async Task Register_Existing_Token_Updates_And_Reactivates_Without_Duplicating ()
        {
            var existing = _repository.AddDevice(_user.Id , TOKEN , isActive: false);

            var response = await Register(Request(TOKEN , "android" , name: "Novo nome"));

            _repository.Devices.Should().ContainSingle();
            response.Id.Should().Be(existing.Id);
            existing.IsActive.Should().BeTrue();
            existing.DeviceName.Should().Be("Novo nome");
            existing.UpdatedAt.Should().Be(_time.UtcNow);
        }

        [Fact]
        public async Task Register_Token_Of_Another_User_Transfers_It ()
        {
            var existing = _repository.AddDevice(userId: 999 , TOKEN);
            existing.LastUsedAt = _time.UtcNow;

            await Register(Request(TOKEN , "android"));

            _repository.Devices.Should().ContainSingle();
            existing.UserId.Should().Be(_user.Id);
            existing.LastUsedAt.Should().BeNull();
        }

        [Fact]
        public async Task Register_New_Token_Of_Same_Install_Retires_Old_Token ()
        {
            var old = _repository.AddDevice(_user.Id , OTHER_TOKEN , deviceId: "device-1");
            var otherInstall = _repository.AddDevice(_user.Id , "ExponentPushToken[cccccccccccc333333]" , deviceId: "device-2");
            var otherUsersInstall = _repository.AddDevice(999 , "ExponentPushToken[dddddddddddd444444]" , deviceId: "device-1");

            await Register(Request(TOKEN , "android" , deviceId: "device-1"));

            old.IsActive.Should().BeFalse();
            otherInstall.IsActive.Should().BeTrue();
            otherUsersInstall.IsActive.Should().BeTrue();
            _repository.Devices.Single(d => d.ExpoPushToken == TOKEN).IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Register_Retries_Once_When_Same_Token_Is_Registered_Concurrently ()
        {
            var commits = 0;
            _unitOfWork.Setup(u => u.Commit(It.IsAny<CancellationToken>())).Returns(() => {
                if ( ++commits == 1 ) {
                    // The concurrent request won the unique index; the real UnitOfWork clears the tracker.
                    _repository.Devices.Clear();
                    _repository.AddDevice(_user.Id , TOKEN);
                    throw new ConflictException();
                }

                return Task.CompletedTask;
            });

            await Register(Request(TOKEN , "android"));

            commits.Should().Be(2);
            _repository.Devices.Should().ContainSingle();
        }

        [Fact]
        public async Task Register_Invalid_Request_Does_Not_Touch_Database ()
        {
            var act = () => Register(Request("token" , "android"));

            (await act.Should().ThrowAsync<ErrorOnValidationException>())
                .Which.GetErrors().Should().ContainSingle().Which.Should().Be(ResourceErrorMessages.PUSH_TOKEN_INVALID);

            _repository.Devices.Should().BeEmpty();
            _unitOfWork.Verify(u => u.Commit(It.IsAny<CancellationToken>()) , Times.Never);
        }

        [Fact]
        public async Task Deactivate_Own_Device ()
        {
            var device = _repository.AddDevice(_user.Id , TOKEN);

            await Deactivate(TOKEN);

            device.IsActive.Should().BeFalse();
            _unitOfWork.Verify(u => u.Commit(It.IsAny<CancellationToken>()) , Times.Once);
        }

        [Fact]
        public async Task Deactivate_Device_Of_Another_User_Is_Not_Found_And_Untouched ()
        {
            var device = _repository.AddDevice(userId: 999 , TOKEN);

            var act = () => Deactivate(TOKEN);

            (await act.Should().ThrowAsync<NotFoundException>())
                .Which.Message.Should().Be(ResourceErrorMessages.PUSH_DEVICE_NOT_FOUND);

            device.IsActive.Should().BeTrue();
            _unitOfWork.Verify(u => u.Commit(It.IsAny<CancellationToken>()) , Times.Never);
        }

        [Fact]
        public async Task Deactivate_Unknown_Token_Is_Not_Found ()
        {
            var act = () => Deactivate(TOKEN);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task List_Returns_Only_Own_Devices_Without_Full_Token ()
        {
            _repository.AddDevice(_user.Id , TOKEN);
            _repository.AddDevice(999 , OTHER_TOKEN);

            var response = await new GetPushDevicesUseCase(_repository , LoggedUserBuilder.Build(_user)).Execute(CancellationToken.None);

            var device = response.Devices.Should().ContainSingle().Subject;
            device.TokenSuffix.Should().Be("…111111");
            device.Platform.Should().Be("android");
        }

        private Task<MiCash.Communication.Responses.PushDevices.ResponseRegisteredPushDeviceJson> Register ( RequestRegisterPushDeviceJson request ) =>
            new RegisterPushDeviceUseCase(
                _repository , _repository , LoggedUserBuilder.Build(_user) , _unitOfWork.Object , _time ,
                NullLogger<RegisterPushDeviceUseCase>.Instance)
            .Execute(request , CancellationToken.None);

        private Task Deactivate ( string token ) =>
            new DeactivatePushDeviceUseCase(_repository , LoggedUserBuilder.Build(_user) , _unitOfWork.Object , _time)
            .Execute(new RequestDeactivatePushDeviceJson { ExpoPushToken = token } , CancellationToken.None);

        private static RequestRegisterPushDeviceJson Request ( string token , string platform , string? deviceId = null , string name = "Pixel" ) => new()
        {
            ExpoPushToken = token ,
            Platform = platform ,
            DeviceId = deviceId ,
            DeviceName = name
        };
    }
}
