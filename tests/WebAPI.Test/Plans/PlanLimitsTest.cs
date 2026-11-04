using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace WebAPI.Test.Plans
{
    public class PlanLimitsTest : MiCashClassFixture
    {
        private readonly string _premiumToken;
        private readonly string _freeToken;

        public PlanLimitsTest ( CustomWebApplicationFactory factory ) : base(factory)
        {
            _premiumToken = factory.GetToken();
            _freeToken = factory.GetTokenWithoutSubscription();
        }

        private static (int Year, int Month) MonthsAgo ( int months )
        {
            // Same calendar as the API (Brasília).
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow , TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).DateTime);
            var date = today.AddMonths(-months);
            return (date.Year, date.Month);
        }

        [Theory]
        [InlineData("api/dashboard/monthly-summary")]
        [InlineData("api/dashboard/daily-summary")]
        [InlineData("api/dashboard/expenses-by-category")]
        [InlineData("api/financial-goal/progress")]
        [InlineData("api/financial-goal/alerts")]
        [InlineData("api/FinancialInsights")]
        public async Task Free_History_Is_Limited_To_3_Previous_Months ( string route )
        {
            var (oldYear, oldMonth) = MonthsAgo(4);
            var (okYear, okMonth) = MonthsAgo(3);

            var blocked = await DoGet($"{route}?year={oldYear}&month={oldMonth}" , _freeToken);
            blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await ReadJson(blocked)).GetProperty("errorCode").GetString().Should().Be("PLAN_LIMIT_REACHED");

            (await DoGet($"{route}?year={okYear}&month={okMonth}" , _freeToken)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await DoGet($"{route}?year={oldYear}&month={oldMonth}" , _premiumToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Limits_Of_Each_Plan ()
        {
            var free = await ReadJson(await DoGet("api/subscription/limits" , _freeToken));
            var premium = await ReadJson(await DoGet("api/subscription/limits" , _premiumToken));

            free.GetProperty("plan").GetString().Should().Be("Free");
            free.GetProperty("maxFinancialGoalsPerMonth").GetInt32().Should().Be(3);
            free.GetProperty("maxSavingsGoals").GetInt32().Should().Be(1);
            free.GetProperty("historyMonths").GetInt32().Should().Be(3);
            free.GetProperty("chat").GetProperty("period").GetString().Should().Be("month");
            free.GetProperty("chat").GetProperty("limit").GetInt32().Should().Be(5);

            premium.GetProperty("plan").GetString().Should().Be("Premium");
            premium.GetProperty("maxFinancialGoalsPerMonth").ValueKind.Should().Be(JsonValueKind.Null);
            premium.GetProperty("historyMonths").ValueKind.Should().Be(JsonValueKind.Null);
            premium.GetProperty("chat").GetProperty("period").GetString().Should().Be("day");
            premium.GetProperty("chat").GetProperty("limit").GetInt32().Should().Be(30);
            premium.GetProperty("chat").GetProperty("perMinute").GetInt32().Should().Be(5);
        }

        [Fact]
        public async Task Limits_Without_Token ()
        {
            (await DoGet("api/subscription/limits" , token: string.Empty)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static async Task<JsonElement> ReadJson ( HttpResponseMessage response ) =>
            JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }
}
