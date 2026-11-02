namespace MiCash.Domain.Services.Jobs
{
    public interface IWelcomeEmailJob
    {
        /// <summary>Sends the welcome e-mail. Throws on temporary provider failures so Hangfire retries.</summary>
        Task ExecuteAsync ( long userId , CancellationToken cancellationToken );
    }

    public interface IPasswordResetEmailJob
    {
        /// <summary>
        /// Generates the code of the request, stores its hash and e-mails it. Only the request id travels
        /// through Hangfire: the code never reaches the job storage.
        /// </summary>
        Task ExecuteAsync ( long passwordResetRequestId , CancellationToken cancellationToken );
    }
}
