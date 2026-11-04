namespace MiCash.Domain.Services.Jobs
{
    /// <summary>Safety net for lost webhooks: syncs overdue active subscriptions with the payment gateway.</summary>
    public interface ISubscriptionReconciliationJob
    {
        Task ExecuteAsync ( CancellationToken cancellationToken );
    }
}
