namespace MiCash.Domain.Services.Jobs
{
    public interface ISubscriptionCancellationJob
    {
        Task ExecuteAsync ( long subscriptionId , CancellationToken cancellationToken );
    }
}
