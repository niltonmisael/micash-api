using MiCash.Domain.Entities;
using MiCash.Domain.Services.Payments.Models;

namespace MiCash.Domain.Services.Payments
{
    public interface IPaymentGateway
    {
        Task<CreateCheckoutResult> CreateSubscriptionCheckoutAsync ( User user , Plan plan ,CancellationToken cancellationToken );

        /// <summary>
        /// Cancels the subscription at the gateway (stops future charges). Idempotent: a subscription that is
        /// already canceled or does not exist at the gateway is treated as success.
        /// </summary>
        Task CancelSubscriptionAsync ( string subscriptionId , CancellationToken cancellationToken );

        /// <summary>Current state of the subscription at the gateway, or null when it does not exist there.</summary>
        Task<GatewaySubscription?> GetSubscriptionAsync ( string subscriptionId , CancellationToken cancellationToken );
    }
}
