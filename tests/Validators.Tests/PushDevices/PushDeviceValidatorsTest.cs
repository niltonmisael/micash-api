using FluentAssertions;
using MiCash.Application.UseCases.PushDevices;
using MiCash.Communication.Requests.PushDevices;
using MiCash.Domain.Services.PushNotifications;
using MiCash.Exception;

namespace Validators.Tests.PushDevices
{
    public class PushDeviceValidatorsTest
    {
        private const string VALID_TOKEN = "ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx]";

        [Theory]
        [InlineData("android")]
        [InlineData("ios")]
        [InlineData("IOS")]
        public void Register_Success ( string platform )
        {
            var result = new ValidatorRegisterPushDevice().Validate(Request(VALID_TOKEN , platform));

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Register_Accepts_New_Expo_Token_Prefix ()
        {
            new ValidatorRegisterPushDevice().Validate(Request("ExpoPushToken[abc-DEF_123]" , "android"))
                .IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("xxxxxxxxxxxxxxxxxxxxxx")]
        [InlineData("ExponentPushToken[]")]
        [InlineData("ExponentPushToken[abc")]
        [InlineData("ExponentPushToken[abc] ")]
        [InlineData("ExponentPushToken[ab c]")]
        [InlineData("fcm:ExponentPushToken[abc]")]
        public void Register_Invalid_Token ( string token )
        {
            var result = new ValidatorRegisterPushDevice().Validate(Request(token , "android"));

            result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(ResourceErrorMessages.PUSH_TOKEN_INVALID);
        }

        [Fact]
        public void Register_Token_Too_Long ()
        {
            var token = $"ExponentPushToken[{new string('a' , 300)}]";

            new ValidatorRegisterPushDevice().Validate(Request(token , "android"))
                .Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(ResourceErrorMessages.PUSH_TOKEN_INVALID);
        }

        [Theory]
        [InlineData("")]
        [InlineData("web")]
        [InlineData("windows")]
        public void Register_Invalid_Platform ( string platform )
        {
            new ValidatorRegisterPushDevice().Validate(Request(VALID_TOKEN , platform))
                .Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(ResourceErrorMessages.PUSH_PLATFORM_INVALID);
        }

        [Fact]
        public void Register_Device_Fields_Too_Long ()
        {
            var request = Request(VALID_TOKEN , "android");
            request.DeviceId = new string('d' , 201);
            request.DeviceName = new string('n' , 101);

            var errors = new ValidatorRegisterPushDevice().Validate(request).Errors.Select(e => e.ErrorMessage);

            errors.Should().BeEquivalentTo([ResourceErrorMessages.PUSH_DEVICE_ID_TOO_LONG , ResourceErrorMessages.PUSH_DEVICE_NAME_TOO_LONG]);
        }

        [Fact]
        public void Deactivate_Invalid_Token ()
        {
            new ValidatorDeactivatePushDevice().Validate(new RequestDeactivatePushDeviceJson { ExpoPushToken = "abc" })
                .Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(ResourceErrorMessages.PUSH_TOKEN_INVALID);
        }

        [Theory]
        [InlineData("/home")]
        [InlineData("/transactions")]
        [InlineData("/goals?tab=limits")]
        [InlineData("/savings-goal?id=5")]
        [InlineData("/new-transaction?type=expense")]
        public void Route_Allowed ( string route )
        {
            PushRoutes.IsAllowed(route).Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("https://evil.example")]
        [InlineData("//evil.example/home")]
        [InlineData("home")]
        [InlineData("/expenses")]
        [InlineData("/home/../admin")]
        [InlineData("/home?next=https://evil.example")]
        [InlineData("/home#x")]
        [InlineData("javascript:alert(1)")]
        [InlineData("/HOME")]
        public void Route_Rejected ( string? route )
        {
            PushRoutes.IsAllowed(route).Should().BeFalse();
        }

        [Fact]
        public void Token_Is_Masked ()
        {
            PushTokenMask.Suffix("ExponentPushToken[abcdefghij123456]").Should().Be("…123456");

            var message = "\"ExponentPushToken[abcdefghij123456]\" is not a registered push notification recipient";

            PushTokenMask.Sanitize(message).Should()
                .NotContain("abcdefghij")
                .And.Contain("ExponentPushToken[…123456]");
        }

        private static RequestRegisterPushDeviceJson Request ( string token , string platform ) => new()
        {
            ExpoPushToken = token ,
            Platform = platform ,
            DeviceId = "device-1" ,
            DeviceName = "Pixel"
        };
    }
}
