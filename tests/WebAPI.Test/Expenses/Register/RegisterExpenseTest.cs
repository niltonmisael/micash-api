using CommonTestUtilities.Requests;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebAPI.Test.Expenses.Register
{
    public class RegisterExpenseTest : IClassFixture<CustomWebApplicationFactory>
    {
        private const string METHOD = "api/Expenses";

        private readonly HttpClient _httpClient;
        private readonly string _token;

        public RegisterExpenseTest(CustomWebApplicationFactory webApplicationFactory)
        {
            _httpClient = webApplicationFactory.CreateClient();
            _token = webApplicationFactory.GetToken();
        }

        [Fact]
        public async Task Success ()
        {
            var request = RequestRegisterExpenseJsonBuilder.Build();

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer" , _token);

            var result = await _httpClient.PostAsJsonAsync(METHOD, request);

            result.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var body = await result.Content.ReadAsStringAsync();

            var response = JsonDocument.Parse(body);

            response.RootElement.GetProperty("title").GetString().Should().Be(request.Title);
            
        }

    }
}
