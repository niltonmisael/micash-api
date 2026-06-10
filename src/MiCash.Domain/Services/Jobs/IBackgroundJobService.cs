namespace MiCash.Domain.Services.Jobs
{
    public interface IBackgroundJobService
    {
        string ScheduleSubscriptionCancellation ( long subscriptionId , DateTime executeAt );

        bool Delete ( string jobId );
    }
}
