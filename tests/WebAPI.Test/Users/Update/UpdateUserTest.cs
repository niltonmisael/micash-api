using CommonTestUtilities.Requests;
using FluentAssertions;
using MiCash.Exception;
using System.Globalization;
using System.Net;
using System.Text.Json;
using WebAPI.Test.InlineData;

namespace WebAPI.Test.Users.Update
{
    public class UpdateUserTest : MiCashClassFixture
    {
        private const string METHOD = "api/user/update";

        private readonly string _token;
        private readonly string _email;

        public UpdateUserTest ( CustomWebApplicationFactory webApplicationFactory ) : base(webApplicationFactory)
        {
            _token = webApplicationFactory.GetToken();
            _email = webApplicationFactory.GetEmail();
        }

        [Fact]
        public async Task Success ()
        {
            var request = RequestUpdateUserJsonBuilder.Build();
            request.Email = _email;

            var response = await DoPut(METHOD , request , token: _token);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Different_Email_Is_400_With_The_Code_For_The_Change_Flow ()
        {
            var request = RequestUpdateUserJsonBuilder.Build();
            request.Email = "outro.endereco@micash.com.br";

            var response = await DoPut(METHOD , request , token: _token);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            body.GetProperty("errorCode").GetString().Should().Be("EMAIL_CHANGE_REQUIRES_CONFIRMATION");
        }

        [Theory]
        [ClassData(typeof(CultureInlineDataTest))]
        public async Task Error_Empty_Name ( string culture )
        {
            var request = RequestUpdateUserJsonBuilder.Build();
            request.Name = string.Empty;

            var response = await DoPut(METHOD , request , token: _token , culture: culture);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            await using var responseBody = await response.Content.ReadAsStreamAsync();

            var responseData = await JsonDocument.ParseAsync(responseBody);

            var errors = responseData.RootElement.GetProperty("errorMessages").EnumerateArray();

            var expectedMessage = ResourceErrorMessages.ResourceManager.GetString("NAME_EMPTY" , new CultureInfo(culture));

            errors.Should().HaveCount(1).And.Contain(c => c.GetString()!.Equals(expectedMessage));
        }
    }
}
