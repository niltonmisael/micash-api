using MiCash.Communication.Requests.Subscriptions;

namespace MiCash.Application.UseCases.Subscriptions.Create
{
    public interface ICreateSubscriptionUseCase
    {
        Task ExecuteAsync(RequestCreateSubscriptionJson request, CancellationToken cancellationToken);
    }
}
