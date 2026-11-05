using CommonTestUtilities.AI;
using FluentAssertions;
using MiCash.Communication.Requests.Chat;
using System.Net;
using System.Text.Json;

namespace WebAPI.Test.FinancialInsights
{
    public class FinancialInsightsAndChatTest : MiCashClassFixture
    {
        private const string INSIGHTS = "api/financial-insights";
        private const string CHAT = "api/Chat/message";

        private readonly string _token;
        private readonly string _tokenWithoutSubscription;
        private readonly DateTime _expenseDate;

        public FinancialInsightsAndChatTest ( CustomWebApplicationFactory webApplicationFactory ) : base(webApplicationFactory)
        {
            _token = webApplicationFactory.GetToken();
            _tokenWithoutSubscription = webApplicationFactory.GetTokenWithoutSubscription();
            _expenseDate = webApplicationFactory.GetDate();
        }

        [Fact]
        public async Task Insights_Preview_Without_Subscription ()
        {
            // The second user only has a Pending checkout: Free plan.
            var result = await DoGet($"{INSIGHTS}?month={_expenseDate.Month}&year={_expenseDate.Year}" , _tokenWithoutSubscription);

            result.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = JsonDocument.Parse(await result.Content.ReadAsStringAsync()).RootElement;
            body.GetProperty("isPreview").GetBoolean().Should().BeTrue();
            body.GetProperty("insights").GetArrayLength().Should().BeLessThanOrEqualTo(1);
            body.GetProperty("artificialInsight").ValueKind.Should().Be(JsonValueKind.Null);
            body.GetProperty("lockedInsightsCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task Insights_Success ()
        {
            var result = await DoGet($"{INSIGHTS}?month={_expenseDate.Month}&year={_expenseDate.Year}" , _token);

            result.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = JsonDocument.Parse(await result.Content.ReadAsStringAsync()).RootElement;

            // Seeded expense: 150 in "Moradia", no incomes => highest category + negative balance.
            var titles = body.GetProperty("insights").EnumerateArray()
                .Select(i => i.GetProperty("title").GetString())
                .ToList();

            titles.Should().Contain("Maior gasto do mês").And.Contain("Gastos acima da renda");

            body.GetProperty("artificialInsight").GetProperty("message").GetString()
                .Should().Be(FakeAIService.DEFAULT_RESPONSE);
        }

        [Fact]
        public async Task Insights_Error_Without_Token ()
        {
            var result = await DoGet($"{INSIGHTS}?month=1&year=2026" , token: string.Empty);

            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Insights_Error_Invalid_Period ()
        {
            var result = await DoGet(INSIGHTS , _token);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Chat_Success ()
        {
            var result = await DoPost(CHAT , new RequestChatMessageJson { Message = "Onde estou gastando mais?" } , _token);

            result.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = JsonDocument.Parse(await result.Content.ReadAsStringAsync()).RootElement;

            body.GetProperty("message").GetString().Should().Be(FakeAIService.DEFAULT_RESPONSE);
        }

        [Fact]
        public async Task Chat_Error_Empty_Message ()
        {
            var result = await DoPost(CHAT , new RequestChatMessageJson { Message = "" } , _token);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Chat_Error_Without_Token ()
        {
            var result = await DoPost(CHAT , new RequestChatMessageJson { Message = "Oi" } , token: string.Empty);

            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
