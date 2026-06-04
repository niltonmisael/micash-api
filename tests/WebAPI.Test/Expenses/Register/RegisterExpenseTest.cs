using CommonTestUtilities.Requests;
using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace WebAPI.Test.Expenses.Register
{
    public class RegisterExpenseTest : MiCashClassFixture
    {
        private const string METHOD = "api/expense";

        private readonly string _token;

        public RegisterExpenseTest(CustomWebApplicationFactory webApplicationFactory): base(webApplicationFactory) 
        {
            _token = webApplicationFactory.User_Team_Member.GetToken();
        }

        [Fact]
        public async Task Success ()
        {
            var request = RequestExpenseJsonBuilder.Build();

            var result = await DoPost(requestUri: METHOD, request: request, token: _token);

            result.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var body = await result.Content.ReadAsStringAsync();

            var response = JsonDocument.Parse(body);

            response.RootElement.GetProperty("title").GetString().Should().Be(request.Title);
            
        }

    }
}
