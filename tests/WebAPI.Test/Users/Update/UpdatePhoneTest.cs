using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace WebAPI.Test.Users.Update
{
    public class UpdatePhoneTest : MiCashClassFixture
    {
        private const string UPDATE = "api/user/update";
        private const string SEEDED_PHONE = "43999999999";

        private readonly string _token;
        private readonly string _email;
        private readonly string _name;

        public UpdatePhoneTest ( CustomWebApplicationFactory factory ) : base(factory)
        {
            _token = factory.GetToken();
            _email = factory.GetEmail();
            _name = factory.GetName();
        }

        [Fact]
        public async Task Phone_Is_Stored_As_Digits_Kept_When_Omitted_And_Validated ()
        {
            try {
                (await DoPut(UPDATE , new { name = _name , email = _email , phone = "+55 (11) 98765-4321" } , _token))
                    .StatusCode.Should().Be(HttpStatusCode.NoContent);
                // Clients that only edit the name (the current mobile app) do not send the phone.
                await DoPut(UPDATE , new { name = _name , email = _email } , _token);
                (await Phone()).Should().Be("11987654321");

                (await DoPut(UPDATE , new { name = _name , email = _email , phone = "1198765432" } , _token))
                    .StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }
            finally {
                await DoPut(UPDATE , new { name = _name , email = _email , phone = SEEDED_PHONE } , _token);
            }
        }

        private async Task<string?> Phone ()
        {
            var json = JsonDocument.Parse(await (await DoGet("api/user/profile" , _token)).Content.ReadAsStringAsync()).RootElement;
            return json.GetProperty("phone").GetString();
        }
    }
}
