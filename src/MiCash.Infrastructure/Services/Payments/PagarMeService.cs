using MiCash.Domain.Entities;
using MiCash.Domain.Services.Payments;
using MiCash.Domain.Services.Payments.Models;
using MiCash.Infrastructure.Services.Payments.Requests;
using MiCash.Infrastructure.Services.Payments.Responses;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace MiCash.Infrastructure.Services.Payments
{
    public class PagarMeService : IPaymentGateway
    {

        private readonly HttpClient _httpClient;
        private readonly PagarMeSettings _settings;

        public PagarMeService ( 
            HttpClient httpClient,
            IOptions<PagarMeSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;

            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Basic" , Convert.ToBase64String(
                        Encoding.UTF8.GetBytes($"{_settings.ApiKey}")));
        }

        public Task CancelSubscriptionAsync ( string subscriptionId , CancellationToken cancellationToken )
        {
            throw new NotImplementedException();
        }

        public async Task<string> CreateCustomerAsync ( User user , CancellationToken cancellationToken )
        {
            var request = new CreateCustomerRequest
            {
                Email = user.Email ,
                Name = user.Name ,
                Type = "individual"
            };

            var response = await _httpClient.PostAsJsonAsync(
                "customers",
                request, cancellationToken );

            if ( !response.IsSuccessStatusCode ) {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);

                throw new Exception($"Erro ao criar cliente Pagar.me: {error}");
            }

            var customer = await response.Content
                .ReadFromJsonAsync<CreateCustomerResponse>(cancellationToken);

            if ( customer is null ) {
                throw new Exception("Não foi possível obter o CustomerId.");
            }

            return customer.Id;
        }

        public Task<CreateCheckoutResult> CreateSubscriptionCheckoutAsync ( User user , Plan plan , CancellationToken cancellationToken )
        {
            throw new NotImplementedException();
        }
    }
}
