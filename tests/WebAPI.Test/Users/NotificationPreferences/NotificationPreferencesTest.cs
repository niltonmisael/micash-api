using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace WebAPI.Test.Users.NotificationPreferences
{
    public class NotificationPreferencesTest : MiCashClassFixture
    {
        private const string METHOD = "api/user/notification-preferences";

        private readonly string _token;

        public NotificationPreferencesTest ( CustomWebApplicationFactory factory ) : base(factory)
        {
            _token = factory.GetToken();
        }

        [Fact]
        public async Task Defaults_Then_Update_And_Read_Back ()
        {
            var initial = await ReadJson(await DoGet(METHOD , _token));
            // Monthly summary on by default; news is opt-in.
            initial.GetProperty("monthlySummaryEmail").GetBoolean().Should().BeTrue();
            initial.GetProperty("monthlySummaryPush").GetBoolean().Should().BeTrue();
            initial.GetProperty("newsEmail").GetBoolean().Should().BeFalse();

            try {
                var put = await DoPut(METHOD , new { monthlySummaryEmail = false , monthlySummaryPush = true , newsEmail = true } , _token);
                put.StatusCode.Should().Be(HttpStatusCode.OK);

                var after = await ReadJson(await DoGet(METHOD , _token));
                after.GetProperty("monthlySummaryEmail").GetBoolean().Should().BeFalse();
                after.GetProperty("monthlySummaryPush").GetBoolean().Should().BeTrue();
                after.GetProperty("newsEmail").GetBoolean().Should().BeTrue();
            }
            finally {
                await DoPut(METHOD , new { monthlySummaryEmail = true , monthlySummaryPush = true , newsEmail = false } , _token);
            }
        }

        private static async Task<JsonElement> ReadJson ( HttpResponseMessage response ) =>
            JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
