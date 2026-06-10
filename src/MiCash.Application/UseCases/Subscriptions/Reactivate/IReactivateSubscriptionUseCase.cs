namespace MiCash.Application.UseCases.Subscriptions.Reactivate
{
    public interface IReactivateSubscriptionUseCase
    {
        Task ExecuteAsync ( CancellationToken cancellationToken );
    }
}
