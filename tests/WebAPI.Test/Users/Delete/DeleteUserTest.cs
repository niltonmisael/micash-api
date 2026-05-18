using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebAPI.Test.Users.Delete
{
    public class DeleteUserTest : MiCashClassFixture
    {
        private const string METHOD = "api/user/delete";

        private readonly string _token;

        public DeleteUserTest ( CustomWebApplicationFactory webApplicationFactory ) : base(webApplicationFactory)
        {
            _token = webApplicationFactory.User_Team_Member.GetToken();
        }

        [Fact]
        public async Task Success ()
        {
            var result = await DoDelete(METHOD , _token);

            result.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }
}
