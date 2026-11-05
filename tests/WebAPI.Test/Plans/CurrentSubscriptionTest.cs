using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebAPI.Test.Plans
{
    /// <summary>
    /// GET api/subscription/current and api/subscription/limits as the mobile apps read them (consumption-only):
    /// the answer comes from the server's records only, is the same for any client, and nothing the client sends
    /// can turn a Free user into Premium.
    /// </summary>
    public class CurrentSubscriptionTest : MiCashClassFixture
    {
        private readonly CustomWebApplicationFactory _factory;

        public CurrentSubscriptionTest ( CustomWebApplicationFactory factory ) : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Active_Subscriber_Is_Premium_With_Renewal_Details ()
        {
            var current = await ReadJson(await DoGet("api/subscription/current" , _factory.GetToken()));

            current.GetProperty("statusDescription").GetString().Should().Be("Active");
            current.GetProperty("isPremium").GetBoolean().Should().BeTrue();
            current.GetProperty("autoRenew").GetBoolean().Should().BeTrue();
            current.GetProperty("cancelAtPeriodEnd").GetBoolean().Should().BeFalse();
            current.GetProperty("accessEndsAt").ValueKind.Should().Be(JsonValueKind.Null, "it renews normally");
            current.GetProperty("billingPeriod").GetString().Should().Be("Monthly");
            current.GetProperty("price").GetDecimal().Should().Be(19.90m);
            current.TryGetProperty("startsAt" , out _).Should().BeTrue();
        }

        [Fact]
        public async Task Pending_Checkout_Is_Not_Premium ()
        {
            var current = await ReadJson(await DoGet("api/subscription/current" , _factory.GetTokenWithoutSubscription()));

            current.GetProperty("statusDescription").GetString().Should().Be("Pending");
            current.GetProperty("isPremium").GetBoolean().Should().BeFalse();
            current.GetProperty("autoRenew").GetBoolean().Should().BeFalse();
        }

        [Theory]
        [InlineData("web")]
        [InlineData("android")]
        [InlineData("ios")]
        public async Task Premium_Is_The_Same_On_Every_Platform ( string platform )
        {
            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer" , _factory.GetToken());
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"MiCash-{platform}/1.0");
            client.DefaultRequestHeaders.Add("X-Client-Platform" , platform);

            var current = await ReadJson(await client.GetAsync("api/subscription/current"));
            var limits = await ReadJson(await client.GetAsync("api/subscription/limits"));

            current.GetProperty("isPremium").GetBoolean().Should().BeTrue();
            limits.GetProperty("isPremium").GetBoolean().Should().BeTrue();
            limits.GetProperty("plan").GetString().Should().Be("Premium");
        }

        [Fact]
        public async Task The_Client_Cannot_Make_Itself_Premium ()
        {
            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer" , _factory.GetTokenWithoutSubscription());
            var fake = new { status = 1 , statusDescription = "Active" , isPremium = true , plan = "Premium" };

            // There is no endpoint that writes the subscription state: these routes only read.
            (await client.PostAsJsonAsync("api/subscription/current" , fake)).StatusCode
                .Should().BeOneOf(HttpStatusCode.MethodNotAllowed , HttpStatusCode.NotFound);
            (await client.PutAsJsonAsync("api/subscription/current" , fake)).StatusCode
                .Should().BeOneOf(HttpStatusCode.MethodNotAllowed , HttpStatusCode.NotFound);
            (await client.PutAsJsonAsync("api/subscription/limits" , fake)).StatusCode
                .Should().BeOneOf(HttpStatusCode.MethodNotAllowed , HttpStatusCode.NotFound);

            // A webhook without Pagar.me's credentials is refused before reaching any handler.
            using var forged = new HttpRequestMessage(HttpMethod.Post , "api/pagar-me-webhook")
            {
                Content = JsonContent.Create(new { id = "hook_forged" , type = "charge.paid" , data = new { id = "ch_x" } })
            };
            forged.Headers.Authorization = new AuthenticationHeaderValue("Basic" , Convert.ToBase64String("x:y"u8.ToArray()));
            (await client.SendAsync(forged)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // Headers claiming another plan change nothing.
            client.DefaultRequestHeaders.Add("X-Plan" , "Premium");
            var current = await ReadJson(await client.GetAsync("api/subscription/current"));
            current.GetProperty("isPremium").GetBoolean().Should().BeFalse();
        }

        [Fact]
        public async Task Plans_Expose_The_Billing_Period ()
        {
            var plans = await ReadJson(await DoGet("api/plan" , _factory.GetToken()));

            plans.EnumerateArray().Should().NotBeEmpty()
                .And.OnlyContain(p => p.GetProperty("billingPeriod").GetString() == "Monthly");
        }

        private static async Task<JsonElement> ReadJson ( HttpResponseMessage response )
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
        }
    }
}
