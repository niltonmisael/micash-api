using CommonTestUtilities.Requests;
using FluentAssertions;
using MiCash.Communication.Requests;
using MiCash.Exception;
using System.Globalization;
using System.Net;
using System.Text.Json;
using WebAPI.Test.InlineData;

namespace WebAPI.Test.Users.ChangePassword
{
    public class ChangePasswordTest : MiCashClassFixture
    {
        private const string METHOD = "api/user/change-password";

        private readonly string _token;
        private readonly string _password;
        private readonly string _email;

        public ChangePasswordTest ( CustomWebApplicationFactory webApplicationFactory ) : base(webApplicationFactory)
        {
            _token = webApplicationFactory.GetToken();
            _password = webApplicationFactory.GetPassword();
            _email = webApplicationFactory.GetEmail();
        }

        [Theory]
        [ClassData(typeof(CultureInlineDataTest))]
        public async Task Error_Password_Different_Current_Password ( string culture )
        {
            var request = RequestChangePasswordJsonBuilder.Build();

            var response = await DoPut(METHOD , request , token: _token , culture: culture);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            await using var responseBody = await response.Content.ReadAsStreamAsync();
            var responseData = await JsonDocument.ParseAsync(responseBody);

            var errors = responseData.RootElement.GetProperty("errorMessages").EnumerateArray();

            var expectedMessage = ResourceErrorMessages.ResourceManager.GetString("PASSWORD_DIFFERENT_CURRENT_PASSWORD" , new CultureInfo(culture));

            errors.Should().HaveCount(1).And.Contain(c => c.GetString()!.Equals(expectedMessage));
        }
    }
}
