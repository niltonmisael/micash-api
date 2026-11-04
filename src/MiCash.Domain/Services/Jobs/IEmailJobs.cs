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

    public interface IEmailVerificationEmailJob
    {
        /// <summary>
        /// Generates the code of the e-mail verification request, stores its hash and e-mails it. Only the
        /// request id travels through Hangfire: the code never reaches the job storage.
        /// </summary>
        Task ExecuteAsync ( long emailVerificationRequestId , CancellationToken cancellationToken );
    }

    public interface IEmailChangedNoticeJob
    {
        /// <summary>
        /// Tells the previous address that the account e-mail was changed (request = the confirmed change).
        /// Throws on temporary provider failures so Hangfire retries.
        /// </summary>
        Task ExecuteAsync ( long emailVerificationRequestId , string previousEmail , CancellationToken cancellationToken );
    }

    public interface IUnverifiedAccountCleanupJob
    {
        /// <summary>Deletes accounts that never confirmed the e-mail within the allowed period (recurring).</summary>
        Task ExecuteAsync ( CancellationToken cancellationToken );
    }
}
