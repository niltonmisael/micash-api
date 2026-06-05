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
    public class DoLoginTest : MiCashClassFixture
    {
        private const string METHOD = "api/auth/login";

        private readonly string _email;
        private readonly string _name;
        private readonly string _password;

        public DoLoginTest (CustomWebApplicationFactory webApplicationFactory) : base (webApplicationFactory) 
        {
           
            _email = webApplicationFactory.GetEmail();
            _name = webApplicationFactory.GetName();
            _password = webApplicationFactory.GetPassword();
            
        }

        //Refazer o teste de sucesso, pois o endpoint de login foi alterado.
        //[Fact]
        //public async Task Success ()
        //{
        //    var request = new RequestLoginJson
        //    {
        //        Email = _email ,
        //        Password = _password
        //    };
        //    var response = await DoPost(METHOD , request);
        //    response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        //    var resposeBody = await response.Content.ReadAsStreamAsync();

        //    var responseData = await JsonDocument.ParseAsync(resposeBody);

        //    responseData.RootElement.GetProperty("name").GetString().Should().Be(_name);
        //    responseData.RootElement.GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();

        //}
    }
}
