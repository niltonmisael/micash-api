namespace MiCash.Domain.Services.Jobs
{
    /// <summary>Recurring (day 1): enqueues one <see cref="IMonthlySummaryJob"/> per user due a summary of last month.</summary>
    public interface IMonthlySummaryDispatchJob
    {
        Task ExecuteAsync ( CancellationToken cancellationToken );
    }

    public interface IMonthlySummaryJob
    {
        /// <summary>
        /// Builds the summary of the month and sends it by e-mail and/or push, as the user chose. Idempotent:
        /// a month already sent is skipped. Throws on temporary e-mail failures so Hangfire retries.
        /// </summary>
        Task ExecuteAsync ( long userId , int year , int month , CancellationToken cancellationToken );
    }
}
