using MiCash.Domain.Entities;
using MiCash.Domain.Services.Payments.Models;

namespace MiCash.Domain.Services.Payments
{
    public interface IPaymentGateway
    {
        Task<string> CreateCustomerAsync ( User user , CancellationToken cancellationToken );

        Task<CreateCheckoutResult> CreateSubscriptionCheckoutAsync ( User user , Plan plan ,CancellationToken cancellationToken );

        Task CancelSubscriptionAsync ( string subscriptionId , CancellationToken cancellationToken );
    }
}
