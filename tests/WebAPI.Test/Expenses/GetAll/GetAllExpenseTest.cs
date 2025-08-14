using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebAPI.Test.Expenses.GetAll
{
    public class GetAllExpenseTest : MiCashClassFixture
    {
        private const string METHOD = "api/Expenses";

        private readonly string _token;
        public GetAllExpenseTest ( CustomWebApplicationFactory webApplicationFactory ) : base(webApplicationFactory)
        {
            _token = webApplicationFactory.GetToken();
        }

        [Fact]
        public async Task Success ()
        {
            var result = await DoGet(requestUri: METHOD , token: _token);

            result.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await result.Content.ReadAsStringAsync();

            var response = JsonDocument.Parse(body);

            response.RootElement.GetProperty("expenses").EnumerateArray().Should().NotBeNullOrEmpty();


        }
    }
}
