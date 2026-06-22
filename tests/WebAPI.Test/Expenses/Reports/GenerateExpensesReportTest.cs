using FluentAssertions;
using System.Net;
using System.Net.Mime;

namespace WebAPI.Test.Expenses.Reports
{
    public class GenerateExpensesReportTest : MiCashClassFixture
    {
        private const string METHOD = "api/report";

        private readonly string _token;
        private readonly DateTime _expenseDate;
        public GenerateExpensesReportTest ( CustomWebApplicationFactory webApplicationFactory ) : base(webApplicationFactory)
        {
            _token = webApplicationFactory.GetToken();
            _expenseDate = webApplicationFactory.GetDate();
        }

        //[Fact]
        //public async Task Success_Pdf ()
        //{
        //    var result = await DoGet(requestUri: $"{METHOD}/pdf?month={_expenseDate:yyyy-MM}" , token: _token);

        //    result.StatusCode.Should().Be(HttpStatusCode.OK);

        //    result.Content.Headers.ContentType.Should().NotBeNull();
        //    result.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Pdf);
        //}

        //[Fact]
        //public async Task Success_Excel ()
        //{
        //    var result = await DoGet(requestUri: $"{METHOD}/excel?month={_expenseDate:yyyy-MM}" , token: _token);

        //    result.StatusCode.Should().Be(HttpStatusCode.OK);

        //    result.Content.Headers.ContentType.Should().NotBeNull();
        //    result.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Octet);
        //}
        
    }
}
