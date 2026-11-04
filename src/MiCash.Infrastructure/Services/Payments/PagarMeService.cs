using MiCash.Domain.Entities;
using MiCash.Domain.Services.Payments;
using MiCash.Domain.Services.Payments.Models;
using MiCash.Infrastructure.Services.Payments.Requests;
using MiCash.Infrastructure.Services.Payments.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace MiCash.Infrastructure.Services.Payments
{
    public class PagarMeService : IPaymentGateway
    {
        private const string SubscriptionsEndpoint = "subscriptions";
        private const string PaymentsLinks = "paymentlinks";

        private readonly HttpClient _httpClient;
        private readonly ILogger<PagarMeService> _logger;

        public PagarMeService (
            HttpClient httpClient,
            IOptions<PagarMeSettings> settings,
            ILogger<PagarMeService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var pagarMeSettings = settings.Value;

            _httpClient.BaseAddress = new Uri(pagarMeSettings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Basic" , Convert.ToBase64String(
                        Encoding.UTF8.GetBytes($"{pagarMeSettings.ApiKey}:")));
        }

        public async Task<GatewaySubscription?> GetSubscriptionAsync ( string subscriptionId , CancellationToken cancellationToken )
        {
            using var response =
                await _httpClient.GetAsync(
                    $"{SubscriptionsEndpoint}/{Uri.EscapeDataString(subscriptionId)}" ,
                    cancellationToken);

            if ( response.StatusCode == HttpStatusCode.NotFound ) {
                return null;
            }

            await EnsureSuccessAsync(response , "consultar assinatura" , cancellationToken);

            var subscription =
                await response.Content.ReadFromJsonAsync<PagarMeSubscriptionResponse>(cancellationToken)
                ?? throw new PaymentGatewayException("Assinatura não retornada pelo Pagar.me.");

            return new GatewaySubscription
            {
                Id = subscription.Id ,
                Status = subscription.Status ,
                NextBillingAt = subscription.NextBillingAt?.UtcDateTime ,
                CanceledAt = subscription.CanceledAt?.UtcDateTime
            };
        }

        public async Task CancelSubscriptionAsync ( string subscriptionId , CancellationToken cancellationToken )
        {
            // Idempotent: Hangfire retries this job, and the subscription may have been canceled at the
            // Pagar.me dashboard in the meantime.
            var current = await GetSubscriptionAsync(subscriptionId , cancellationToken);

            if ( current is null ) {
                _logger.LogWarning(
                    "Subscription {SubscriptionId} não existe no Pagar.me; nada a cancelar." ,
                    subscriptionId);

                return;
            }

            if ( current.IsCanceled ) {
                _logger.LogInformation(
                    "Subscription {SubscriptionId} já está cancelada no Pagar.me." ,
                    subscriptionId);

                return;
            }

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Delete ,
                    $"{SubscriptionsEndpoint}/{Uri.EscapeDataString(subscriptionId)}")
                {
                    // Pending invoices would still be charged after the cancellation.
                    Content = JsonContent.Create(new { cancel_pending_invoices = true })
                };

            using var response = await _httpClient.SendAsync(request , cancellationToken);

            if ( response.StatusCode == HttpStatusCode.NotFound ) {
                return;
            }

            await EnsureSuccessAsync(response , "cancelar assinatura" , cancellationToken);
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

            using var response =
                await _httpClient.PostAsJsonAsync(
                    PaymentsLinks ,
                    request ,
                    cancellationToken);

            await EnsureSuccessAsync(response , "criar checkout" , cancellationToken);

            var checkout =
                await response.Content
                    .ReadFromJsonAsync<CreatePaymentLinkResponse>(
                        cancellationToken)
                ?? throw new PaymentGatewayException("Checkout não retornado pelo Pagar.me.");

            return new CreateCheckoutResult
            {
                PaymentLinkId = checkout.Id,
                CheckoutUrl = checkout.Url,
                OrderCode = orderCode ,
            };
        }

        private async Task EnsureSuccessAsync (
            HttpResponseMessage response ,
            string operation ,
            CancellationToken cancellationToken )
        {
            if ( response.IsSuccessStatusCode ) {
                return;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            // The gateway body goes to the logs only; the API client gets the generic error.
            _logger.LogError(
                "Pagar.me falhou ao {Operation}. Status={StatusCode} Body={Body}" ,
                operation ,
                ( int ) response.StatusCode ,
                body);

            throw new PaymentGatewayException(
                $"Pagar.me falhou ao {operation} (HTTP {( int ) response.StatusCode})." ,
                response.StatusCode);
        }
    }
}
