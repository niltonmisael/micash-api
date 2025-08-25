using CommonTestUtilities.Requests;
using FluentAssertions;
using MiCash.Exception;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebAPI.Test.InlineData;

namespace WebAPI.Test.Expenses.Update
{
    public class UpdateExpenseTest : MiCashClassFixture
    {
        private const string METHOD = "api/Expenses";

        private readonly string _token;
        private readonly long _expenseId;
        public UpdateExpenseTest ( CustomWebApplicationFactory webApplicationFactory ) : base(webApplicationFactory)
        {
            _token = webApplicationFactory.User_Team_Member.GetToken();
            _expenseId = webApplicationFactory.Expense_MemberTeam.GetExpenseId();
        }
        [Fact]
        public async Task Success ()
        {
            var request = RequestExpenseJsonBuilder.Build();

            var result = await DoPut(requestUri: $"{METHOD}/{_expenseId}" , request: request , token: _token);

            result.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Theory]
        [ClassData(typeof(CultureInlineDataTest))]
        public async Task Error_Title_Empty ( string culture )
        {
            var request = RequestExpenseJsonBuilder.Build();
            request.Title = string.Empty;

            var result = await DoPut(requestUri: $"{METHOD}/{_expenseId}" , request: request , token: _token , culture: culture);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await result.Content.ReadAsStreamAsync();
            var response = await JsonDocument.ParseAsync(body);

            var errors = response.RootElement.GetProperty("errorMessages").EnumerateArray();

            var expectedMessage = ResourseErrorMessages.ResourceManager.GetString("TITLE_REQUIRED" , new CultureInfo(culture));

            errors.Should().HaveCount(1).And.Contain(error => error.GetString()!.Equals(expectedMessage));
        }

        [Theory]
        [ClassData(typeof(CultureInlineDataTest))]
        public async Task Error_Expense_Not_Found ( string culture )
        {
            var request = RequestExpenseJsonBuilder.Build();

            var result = await DoPut(requestUri: $"{METHOD}/1000" , request: request , token: _token , culture: culture);

            result.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var body = await result.Content.ReadAsStreamAsync();
            var response = await JsonDocument.ParseAsync(body);

            var errors = response.RootElement.GetProperty("errorMessages").EnumerateArray();

            var expectedMessage = ResourseErrorMessages.ResourceManager.GetString("EXPENSE_NOT_FOUND" , new CultureInfo(culture));

            errors.Should().HaveCount(1).And.Contain(error => error.GetString()!.Equals(expectedMessage));
        }
    }
}
