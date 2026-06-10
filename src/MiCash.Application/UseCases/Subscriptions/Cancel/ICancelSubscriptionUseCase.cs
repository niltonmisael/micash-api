namespace MiCash.Application.UseCases.Subscriptions.Cancel
{
    public interface ICancelSubscriptionUseCase
    {
        Task ExecuteAsync ( CancellationToken cancellationToken );
    }
}
