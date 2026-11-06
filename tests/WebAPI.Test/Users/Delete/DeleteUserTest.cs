using FluentAssertions;
using MiCash.Communication.Requests.Users;
using System.Net;

namespace WebAPI.Test.Users.Delete
{
    public class DeleteUserTest : MiCashClassFixture
    {
        private const string METHOD = "api/user/delete";

        private readonly CustomWebApplicationFactory _factory;

        public DeleteUserTest ( CustomWebApplicationFactory webApplicationFactory ) : base(webApplicationFactory)
        {
            _factory = webApplicationFactory;
        }

        [Fact]
        public async Task Wrong_Password_Keeps_The_Account_Then_The_Right_One_Deletes_It ()
        {
            var token = _factory.GetToken();

            var wrong = await DoDelete(METHOD , new RequestDeleteAccountJson { Password = "Errada@123" } , token);
            wrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var deleted = await DoDelete(METHOD , new RequestDeleteAccountJson { Password = _factory.GetPassword() } , token);
            deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
            _factory.BackgroundJobs.EnqueuedAccountDeletedEmails.Should().Contain(e => e.Email == _factory.GetEmail());

            // The access token is still valid, but the account is gone: 401, not a 500.
            var afterwards = await DoDelete(METHOD , new RequestDeleteAccountJson { Password = _factory.GetPassword() } , token);
            afterwards.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
