using MiCash.Communication.Requests.Subscriptions;
using MiCash.Communication.Responses.Subscriptions;

namespace MiCash.Application.UseCases.Subscriptions.CreateCheckout
{
    public interface ICreateSubscriptionCheckoutUseCase
    {
        Task<ResponseSubscriptionCheckoutJson> ExecuteAsync ( RequestCreateSubscriptionCheckoutJson request , CancellationToken cancellationToken );
    }
}
