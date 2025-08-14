using FluentAssertions;
using MiCash.Communication.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebAPI.Test.Login.DoLogin
{
    public class DoLoginTest : IClassFixture<CustomWebApplicationFactory>
    {
        private const string METHOD = "api/Login";

        private readonly HttpClient _httpClient;
        private readonly string _email;
        private readonly string _name;
        private readonly string _password;

        public DoLoginTest (CustomWebApplicationFactory webApplicationFactory)
        {
            _httpClient = webApplicationFactory.CreateClient ();
            _email = webApplicationFactory.GetEmail();
            _name = webApplicationFactory.GetName();
            _password = webApplicationFactory.GetPassword();
        }

        [Fact]
        public async Task Success ()
        {
            var request = new RequestLoginJson
            {
                Email = _email ,
                Password = _password
            };
            var response = await _httpClient.PostAsJsonAsync(METHOD , request);
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var resposeBody = await response.Content.ReadAsStreamAsync();

            var responseData = await JsonDocument.ParseAsync(resposeBody);

            responseData.RootElement.GetProperty("name").GetString().Should().Be(_name);
            responseData.RootElement.GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();

        }
    }
}
