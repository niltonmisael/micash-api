namespace MiCash.Domain.Services.Jobs
{
    /// <summary>Ends the access of a canceled subscription when the paid period is over (back to Free).</summary>
    public interface ISubscriptionExpirationJob
    {
        Task ExecuteAsync ( long subscriptionId , CancellationToken cancellationToken );
    }
}
