using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace WebAPI.Test
{
    public class MiCashClassFixture : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _httpClient;

        public MiCashClassFixture ( CustomWebApplicationFactory webApplicationFactory )
        {
            _httpClient = webApplicationFactory.CreateClient();
        }

        protected async Task<HttpResponseMessage> DoPost (string requestUri, object request, string token = "", string culture = "pt-BR")
        {

            AuthorizeRequest(token);
            ChangeRequestCulture(culture);

            return await _httpClient.PostAsJsonAsync(requestUri , request);
            
        }

        private void AuthorizeRequest ( string token )
        {
            if ( string.IsNullOrEmpty(token) ) {
                return;
            }
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer" , token);

        }

        private void ChangeRequestCulture(string culture )
        {
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Clear();
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(culture));
        }
    }
}
