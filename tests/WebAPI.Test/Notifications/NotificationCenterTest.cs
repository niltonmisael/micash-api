using FluentAssertions;
using MiCash.Domain.Services.PushNotifications;
using MiCash.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace WebAPI.Test.Notifications
{
    public class NotificationCenterTest : MiCashClassFixture
    {
        private const string BASE = "api/notification";

        private readonly CustomWebApplicationFactory _factory;
        private readonly string _token;
        private readonly string _otherToken;

        public NotificationCenterTest ( CustomWebApplicationFactory factory ) : base(factory)
        {
            _factory = factory;
            _token = factory.GetToken();
            _otherToken = factory.GetTokenWithoutSubscription();
        }

        [Fact]
        public async Task List_Count_Read_And_Read_All ()
        {
            // The user has no registered device: notifications are still recorded for the center.
            var ids = new List<long>();
            for ( var i = 1; i <= 3; i++ ) {
                ids.Add(await Notify(_factory.GetEmail() , $"Aviso {i}" , route: i == 1 ? "/goals" : null));
            }

            (await UnreadCount(_token)).Should().Be(3);

            // Newest first, paged by cursor.
            var first = await ReadJson(await DoGet($"{BASE}?limit=2" , _token));
            Ids(first).Should().Equal(ids[2] , ids[1]);
            first.GetProperty("hasMore").GetBoolean().Should().BeTrue();

            var second = await ReadJson(await DoGet($"{BASE}?limit=2&before={ids[1]}" , _token));
            Ids(second).Should().Equal(ids[0]);
            second.GetProperty("hasMore").GetBoolean().Should().BeFalse();

            var oldest = second.GetProperty("notifications")[0];
            oldest.GetProperty("title").GetString().Should().Be("Aviso 1");
            oldest.GetProperty("type").GetString().Should().Be("test");
            oldest.GetProperty("url").GetString().Should().Be("/goals");
            oldest.GetProperty("readAt").ValueKind.Should().Be(JsonValueKind.Null);
            first.GetProperty("notifications")[0].GetProperty("url").ValueKind.Should().Be(JsonValueKind.Null);

            // Mark one as read; again keeps the original time.
            (await DoPut($"{BASE}/{ids[0]}/read" , new { } , _token)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            var readAt = await ReadAtInDb(ids[0]);
            readAt.Should().NotBeNull();
            (await DoPut($"{BASE}/{ids[0]}/read" , new { } , _token)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await ReadAtInDb(ids[0])).Should().Be(readAt);
            (await UnreadCount(_token)).Should().Be(2);

            // Another user cannot see nor mark it.
            var other = await DoPut($"{BASE}/{ids[1]}/read" , new { } , _otherToken);
            other.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await ReadJson(other)).GetProperty("errorMessages")[0].GetString().Should().Be("Notificação não encontrada.");
            (await DoGet(BASE , _otherToken)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await UnreadCount(_otherToken)).Should().Be(0);

            // Read all.
            (await DoPut($"{BASE}/read-all" , new { } , _token)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await UnreadCount(_token)).Should().Be(0);
            (await ReadAtInDb(ids[0])).Should().Be(readAt , "already read notifications keep their time");
        }

        [Fact]
        public async Task Unknown_Notification_Is_404 ()
        {
            (await DoPut($"{BASE}/999999/read" , new { } , _token)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Test_Push_Without_Device_Is_400_And_Not_Recorded ()
        {
            var before = await UnreadCount(_otherToken);

            var response = await DoPost("api/device/push-token/test" , new { } , _otherToken);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await UnreadCount(_otherToken)).Should().Be(before);
        }

        [Fact]
        public async Task Without_Token_Is_401 ()
        {
            (await DoGet($"{BASE}/unread-count" , token: string.Empty)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private async Task<long> Notify ( string email , string title , string? route )
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MiCashDbContext>();
            var userId = await db.Users.Where(u => u.Email == email).Select(u => u.Id).SingleAsync();

            var result = await scope.ServiceProvider.GetRequiredService<IPushNotificationService>()
                .EnqueueAsync(userId , new PushMessage("test" , title , "Corpo" , route) , CancellationToken.None);

            result.Should().NotBeNull();
            return result!.NotificationId;
        }

        private async Task<DateTime?> ReadAtInDb ( long id )
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MiCashDbContext>();
            return await db.PushNotifications.AsNoTracking().Where(n => n.Id == id).Select(n => n.ReadAt).SingleAsync();
        }

        private async Task<int> UnreadCount ( string token ) =>
            (await ReadJson(await DoGet($"{BASE}/unread-count" , token))).GetProperty("count").GetInt32();

        private static IEnumerable<long> Ids ( JsonElement page ) =>
            page.GetProperty("notifications").EnumerateArray().Select(n => n.GetProperty("id").GetInt64()).ToList();

        private static async Task<JsonElement> ReadJson ( HttpResponseMessage response ) =>
            JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
