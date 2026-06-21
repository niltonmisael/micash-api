using MiCash.Communication.Responses.Subscriptions;

namespace MiCash.Application.UseCases.Subscriptions.History
{
    public interface ISubscriptionHistoryUseCase
    {
        Task<IReadOnlyCollection<ResponseSubscriptionHistoryJson>> ExecuteAsync (CancellationToken cancellationToken );
    }
}
