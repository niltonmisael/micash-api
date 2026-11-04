using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace WebAPI.Test.Dashboards
{
    public class MonthForecastTest : MiCashClassFixture
    {
        private const string FORECAST = "api/dashboard/month-forecast";

        private readonly string _token;
        private readonly string _tokenWithoutSubscription;

        public MonthForecastTest ( CustomWebApplicationFactory factory ) : base(factory)
        {
            _token = factory.GetToken();
            _tokenWithoutSubscription = factory.GetTokenWithoutSubscription();
        }

        [Fact]
        public async Task Success_Matches_The_Daily_Summary_Until_Today ()
        {
            var result = await DoGet(FORECAST , _token);

            result.StatusCode.Should().Be(HttpStatusCode.OK);

            var forecast = await ReadJson(result);
            var year = forecast.GetProperty("year").GetInt32();
            var month = forecast.GetProperty("month").GetInt32();
            var today = forecast.GetProperty("today").GetInt32();

            var daily = await ReadJson(await DoGet($"api/dashboard/daily-summary?year={year}&month={month}" , _token));
            var days = daily.GetProperty("days").EnumerateArray().ToList();

            forecast.GetProperty("daysInMonth").GetInt32().Should().Be(days.Count);
            forecast.GetProperty("expenseToDate").GetDecimal()
                .Should().Be(days[today - 1].GetProperty("cumulativeExpense").GetDecimal());
            forecast.GetProperty("income").GetDecimal()
                .Should().Be(days[^1].GetProperty("cumulativeIncome").GetDecimal());
        }

        [Fact]
        public async Task Error_Without_Subscription ()
        {
            (await DoGet(FORECAST , _tokenWithoutSubscription)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Error_Without_Token ()
        {
            (await DoGet(FORECAST , token: string.Empty)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static async Task<JsonElement> ReadJson ( HttpResponseMessage response ) =>
            JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }
}
