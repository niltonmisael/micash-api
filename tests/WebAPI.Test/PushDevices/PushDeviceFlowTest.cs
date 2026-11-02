using FluentAssertions;
using MiCash.Communication.Requests.PushDevices;
using MiCash.Domain.Enums;
using MiCash.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace WebAPI.Test.PushDevices
{
    public class PushDeviceFlowTest : MiCashClassFixture
    {
        private const string METHOD = "api/device/push-token";
        private const string LIST = "api/device/push-tokens";

        private readonly CustomWebApplicationFactory _factory;
        private readonly string _token;
        private readonly string _otherUserToken;

        public PushDeviceFlowTest ( CustomWebApplicationFactory factory ) : base(factory)
        {
            _factory = factory;
            _token = factory.GetToken();
            _otherUserToken = factory.GetTokenWithoutSubscription();
        }

        [Fact]
        public async Task Register_List_Test_And_Deactivate ()
        {
            const string pushToken = "ExponentPushToken[flowAAAAAAAAAA123456]";

            var first = await DoPost(METHOD , Register(pushToken) , _token);
            first.StatusCode.Should().Be(HttpStatusCode.OK);
            var id = (await ReadJson(first)).GetProperty("id").GetInt64();

            // Registering again (e.g. every app start) updates the same row.
            var again = await DoPost(METHOD , Register(pushToken , name: "Pixel 9") , _token);
            (await ReadJson(again)).GetProperty("id").GetInt64().Should().Be(id);

            var list = await DoGet(LIST , _token);
            list.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await list.Content.ReadAsStringAsync();
            body.Should().NotContain("flowAAAAAAAAAA" , "the full token is never returned");

            var device = JsonDocument.Parse(body).RootElement.GetProperty("devices").EnumerateArray().Single(d => d.GetProperty("id").GetInt64() == id);
            device.GetProperty("tokenSuffix").GetString().Should().Be("…123456");
            device.GetProperty("deviceName").GetString().Should().Be("Pixel 9");
            device.GetProperty("platform").GetString().Should().Be("android");
            device.GetProperty("isActive").GetBoolean().Should().BeTrue();

            // Test notification: queued for the user's own active devices, sent by Hangfire.
            var test = await DoPost($"{METHOD}/test" , new { } , _token);
            test.StatusCode.Should().Be(HttpStatusCode.Accepted);
            var notificationId = (await ReadJson(test)).GetProperty("notificationId").GetInt64();
            _factory.BackgroundJobs.EnqueuedPushNotifications.Should().Contain(notificationId);

            await using ( var scope = _factory.Services.CreateAsyncScope() ) {
                var db = scope.ServiceProvider.GetRequiredService<MiCashDbContext>();
                var notification = await db.PushNotifications.Include(n => n.Deliveries).SingleAsync(n => n.Id == notificationId);
                notification.Deliveries.Should().ContainSingle(d => d.DeviceId == id)
                    .Which.Status.Should().Be(PushDeliveryStatus.Pending);
                notification.Data.Should().Contain("\"url\":\"/home\"");
            }

            // Deactivate (before logout).
            (await DoDelete(METHOD , new RequestDeactivatePushDeviceJson { ExpoPushToken = pushToken } , _token))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);

            var afterDeactivate = JsonDocument.Parse(await (await DoGet(LIST , _token)).Content.ReadAsStringAsync())
                .RootElement.GetProperty("devices").EnumerateArray().Single(d => d.GetProperty("id").GetInt64() == id);
            afterDeactivate.GetProperty("isActive").GetBoolean().Should().BeFalse();
        }

        [Fact]
        public async Task Another_User_Cannot_Deactivate_Or_See_The_Device ()
        {
            const string pushToken = "ExponentPushToken[ownerBBBBBBBBBB654321]";

            (await DoPost(METHOD , Register(pushToken) , _token)).StatusCode.Should().Be(HttpStatusCode.OK);

            var deactivate = await DoDelete(METHOD , new RequestDeactivatePushDeviceJson { ExpoPushToken = pushToken } , _otherUserToken);
            deactivate.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await ReadJson(deactivate)).GetProperty("errorMessages")[0].GetString().Should().Be("Aparelho não encontrado.");

            var otherList = await DoGet(LIST , _otherUserToken);
            if ( otherList.StatusCode == HttpStatusCode.OK ) {
                (await otherList.Content.ReadAsStringAsync()).Should().NotContain("654321");
            }

            var ownerDevice = JsonDocument.Parse(await (await DoGet(LIST , _token)).Content.ReadAsStringAsync())
                .RootElement.GetProperty("devices").EnumerateArray().Single(d => d.GetProperty("tokenSuffix").GetString() == "…654321");
            ownerDevice.GetProperty("isActive").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task Same_Phone_Logged_In_With_Another_Account_Transfers_The_Token ()
        {
            const string pushToken = "ExponentPushToken[sharedCCCCCCCCC777777]";

            (await DoPost(METHOD , Register(pushToken) , _token)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await DoPost(METHOD , Register(pushToken) , _otherUserToken)).StatusCode.Should().Be(HttpStatusCode.OK);

            var ownerList = await DoGet(LIST , _token);
            if ( ownerList.StatusCode == HttpStatusCode.OK ) {
                (await ownerList.Content.ReadAsStringAsync()).Should().NotContain("777777");
            }

            (await (await DoGet(LIST , _otherUserToken)).Content.ReadAsStringAsync()).Should().Contain("777777");
        }

        [Fact]
        public async Task Invalid_Request_Is_400_In_Portuguese ()
        {
            var result = await DoPost(METHOD , new RequestRegisterPushDeviceJson { ExpoPushToken = "abc" , Platform = "web" } , _token);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var errors = (await ReadJson(result)).GetProperty("errorMessages").EnumerateArray().Select(e => e.GetString());
            errors.Should().BeEquivalentTo(["Token de notificação inválido." , "A plataforma deve ser android ou ios."]);
        }

        [Fact]
        public async Task Without_Token_Is_401 ()
        {
            (await DoGet(LIST , token: string.Empty)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static RequestRegisterPushDeviceJson Register ( string token , string name = "MiCash Pixel" ) => new()
        {
            ExpoPushToken = token ,
            Platform = "android" ,
            DeviceId = null ,
            DeviceName = name
        };

        private static async Task<JsonElement> ReadJson ( HttpResponseMessage response ) =>
            JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
