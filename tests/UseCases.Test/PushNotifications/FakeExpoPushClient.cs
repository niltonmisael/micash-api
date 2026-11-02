using MiCash.Domain.Services.PushNotifications;

namespace UseCases.Test.PushNotifications
{
    /// <summary>Expo client whose answers the test decides; records every request.</summary>
    internal sealed class FakeExpoPushClient : IExpoPushClient
    {
        public List<IReadOnlyList<ExpoPushMessage>> SendCalls { get; } = [];

        public List<IReadOnlyList<string>> ReceiptCalls { get; } = [];

        /// <summary>Default: every message gets an ok ticket "ticket-{n}".</summary>
        public Func<IReadOnlyList<ExpoPushMessage> , IReadOnlyList<ExpoPushTicket>> OnSend { get; set; }

        public Func<IReadOnlyList<string> , IReadOnlyDictionary<string , ExpoPushReceipt>> OnReceipts { get; set; } =
            _ => new Dictionary<string , ExpoPushReceipt>();

        private int _tickets;

        public FakeExpoPushClient ()
        {
            OnSend = messages => messages.Select(_ => new ExpoPushTicket(true , $"ticket-{++_tickets}" , null , null)).ToList();
        }

        public Task<IReadOnlyList<ExpoPushTicket>> SendAsync ( IReadOnlyList<ExpoPushMessage> messages , CancellationToken cancellationToken )
        {
            SendCalls.Add(messages);

            return Task.FromResult(OnSend(messages));
        }

        public Task<IReadOnlyDictionary<string , ExpoPushReceipt>> GetReceiptsAsync ( IReadOnlyList<string> ticketIds , CancellationToken cancellationToken )
        {
            ReceiptCalls.Add(ticketIds);

            return Task.FromResult(OnReceipts(ticketIds));
        }
    }
}
