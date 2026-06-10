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

        private const string CustomersEndpoint = "customers";
        private const string SubscriptionsEndpoint = "subscriptions";
        private const string PaymentsLinks = "paymentlinks";

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
                        Encoding.UTF8.GetBytes($"{_settings.ApiKey}:")));
        }

        public Task CancelSubscriptionAsync ( string subscriptionId , CancellationToken cancellationToken )
        {
            throw new NotImplementedException();
        }


        public async Task<CreateCheckoutResult> CreateSubscriptionCheckoutAsync ( User user , Plan plan , CancellationToken cancellationToken )
        {

            var orderCode = $"micash_{user.Id}_{Guid.NewGuid():N}";

            var request = new CreatePaymentLinkRequest
            {
                Name = plan.Name ,

                Type = "subscription" ,

                OrderCode = orderCode ,

                IsBuilding = false ,

                Metadata = new Dictionary<string , string>
                {
                    ["user_identifier"] = user.UserIdentifier.ToString() ,
                    ["plan_id"] = plan.Id.ToString()
                } ,

                PaymentSettings = new PaymentSettings
                {
                    AcceptedPaymentMethods =
                    [
                        "credit_card"
                    ] ,

                    CreditCardSettings = new CreditCardSettings
                    {
                        OperationType = "auth_and_capture"
                    }
                } ,

                CartSettings = new PaymentLinkCartSettings
                {
                    Recurrences =
                    [
            
                        new PaymentLinkRecurrence
                        {
                            PlanId = plan.PagarmePlanId
                        }
                    ]
                }
            };

            var response =
                await _httpClient.PostAsJsonAsync(
                    PaymentsLinks ,
                    request ,
                    cancellationToken);

            if ( !response.IsSuccessStatusCode ) {
                var error =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                throw new Exception(
                    $"Erro ao criar checkout: {error}");
            }

            var checkout =
                await response.Content
                    .ReadFromJsonAsync<CreatePaymentLinkResponse>(
                        cancellationToken);

            if ( checkout is null ) {
                throw new Exception(
                    "Checkout não retornado pelo Pagar.me.");
            }

            return new CreateCheckoutResult
            {
                PaymentLinkId = checkout.Id,
                CheckoutUrl = checkout.Url,
                OrderCode = orderCode ,
            };
        }
    }
}
