using MiCash.Domain.Services.Email;
using System.Collections.Concurrent;

namespace CommonTestUtilities.Email
{
    /// <summary>Records the e-mails instead of calling Resend (no real sends in tests).</summary>
    public sealed class FakeEmailService : IEmailService
    {
        public ConcurrentQueue<EmailMessage> Sent { get; } = new();

        public Task<string> SendAsync ( EmailMessage message , CancellationToken cancellationToken = default )
        {
            Sent.Enqueue(message);
            return Task.FromResult(Guid.NewGuid().ToString());
        }
    }
}
