using MiCash.Communication.Responses.Subscriptions;

namespace MiCash.Application.UseCases.Subscriptions.GetCurrent
{
    public interface IGetCurrentSubscriptionUseCase
    {
        Task<ResponseCurrentSubscriptionJson> ExecuteAsync (CancellationToken cancellationToken);
    }
}
