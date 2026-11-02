using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.PushNotifications;

namespace CommonTestUtilities.Repositories
{
    /// <summary>
    /// List-backed push repositories with the same filters as the EF implementation, so job tests
    /// observe real state transitions (e.g. a re-run doesn't pick already-claimed deliveries).
    /// </summary>
    public sealed class InMemoryPushRepository :
        IPushDeviceReadOnlyRepository,
        IPushDeviceWriteOnlyRepository,
        IPushDeviceUpdateOnlyRepository,
        IPushNotificationReadOnlyRepository,
        IPushNotificationWriteOnlyRepository,
        IPushDeliveryUpdateOnlyRepository
    {
        private long _nextId = 1;

        public List<PushDevice> Devices { get; } = [];

        public List<PushNotification> Notifications { get; } = [];

        public IEnumerable<PushNotificationDelivery> Deliveries => Notifications.SelectMany(n => n.Deliveries);

        public PushDevice AddDevice ( long userId , string token , bool isActive = true , string? deviceId = null )
        {
            var device = new PushDevice
            {
                Id = _nextId++ ,
                UserId = userId ,
                ExpoPushToken = token ,
                Platform = DevicePlatform.Android ,
                DeviceId = deviceId ,
                IsActive = isActive
            };

            Devices.Add(device);

            return device;
        }

        public PushNotificationDelivery AddDelivery ( PushDevice device , DateTime now , string data = """{"type":"test","url":"/home"}""" )
        {
            var notification = new PushNotification
            {
                Id = _nextId++ ,
                UserId = device.UserId ,
                Type = "test" ,
                Title = "Título" ,
                Body = "Corpo" ,
                Data = data ,
                CreatedAt = now
            };

            var delivery = new PushNotificationDelivery
            {
                Id = _nextId++ ,
                Notification = notification ,
                NotificationId = notification.Id ,
                Device = device ,
                DeviceId = device.Id ,
                NextAttemptAt = now ,
                CreatedAt = now ,
                UpdatedAt = now
            };

            notification.Deliveries.Add(delivery);
            Notifications.Add(notification);

            return delivery;
        }

        public Task<IReadOnlyList<PushDevice>> GetAllByUserAsync ( long userId , CancellationToken cancellationToken ) =>
            Result(Devices.Where(d => d.UserId == userId));

        public Task<IReadOnlyList<PushDevice>> GetActiveByUserAsync ( long userId , CancellationToken cancellationToken ) =>
            Result(Devices.Where(d => d.UserId == userId && d.IsActive));

        public Task AddAsync ( PushDevice device , CancellationToken cancellationToken )
        {
            device.Id = _nextId++;
            Devices.Add(device);

            return Task.CompletedTask;
        }

        public Task<PushDevice?> GetByTokenAsync ( string expoPushToken , CancellationToken cancellationToken ) =>
            Task.FromResult(Devices.FirstOrDefault(d => d.ExpoPushToken == expoPushToken));

        public Task<IReadOnlyList<PushDevice>> GetActiveByDeviceIdAsync ( long userId , string deviceId , CancellationToken cancellationToken ) =>
            Result(Devices.Where(d => d.UserId == userId && d.DeviceId == deviceId && d.IsActive));

        public Task<bool> ExistsAsync ( long userId , string idempotencyKey , CancellationToken cancellationToken ) =>
            Task.FromResult(Notifications.Any(n => n.UserId == userId && n.IdempotencyKey == idempotencyKey));

        public Task AddAsync ( PushNotification notification , CancellationToken cancellationToken )
        {
            notification.Id = _nextId++;

            foreach ( var delivery in notification.Deliveries ) {
                delivery.Id = _nextId++;
                delivery.Notification = notification;
                delivery.NotificationId = notification.Id;
                delivery.Device = Devices.First(d => d.Id == delivery.DeviceId);
            }

            Notifications.Add(notification);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PushNotificationDelivery>> GetDueAsync ( DateTime now , long? notificationId , int take , CancellationToken cancellationToken ) =>
            Result(Deliveries
                .Where(d => d.Status == PushDeliveryStatus.Pending && d.NextAttemptAt <= now)
                .Where(d => notificationId == null || d.NotificationId == notificationId)
                .OrderBy(d => d.Id)
                .Take(take));

        public Task<IReadOnlyList<PushNotificationDelivery>> GetAwaitingReceiptAsync ( DateTime sentBefore , int take , CancellationToken cancellationToken ) =>
            Result(Deliveries
                .Where(d => d.Status == PushDeliveryStatus.SentToExpo && d.SentAt <= sentBefore)
                .OrderBy(d => d.ReceiptCheckedAt.HasValue)
                .ThenBy(d => d.ReceiptCheckedAt)
                .ThenBy(d => d.Id)
                .Take(take));

        public Task<IReadOnlyList<PushNotificationDelivery>> GetStuckSendingAsync ( DateTime claimedBefore , int take , CancellationToken cancellationToken ) =>
            Result(Deliveries
                .Where(d => d.Status == PushDeliveryStatus.Sending && d.ClaimedAt <= claimedBefore)
                .OrderBy(d => d.Id)
                .Take(take));

        private static Task<IReadOnlyList<T>> Result<T> ( IEnumerable<T> items ) =>
            Task.FromResult<IReadOnlyList<T>>(items.ToList());
    }
}
