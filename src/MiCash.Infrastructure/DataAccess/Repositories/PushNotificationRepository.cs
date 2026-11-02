using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.PushNotifications;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess.Repositories
{
    public sealed class PushNotificationRepository :
        IPushDeviceReadOnlyRepository,
        IPushDeviceWriteOnlyRepository,
        IPushDeviceUpdateOnlyRepository,
        IPushNotificationReadOnlyRepository,
        IPushNotificationWriteOnlyRepository,
        INotificationCenterReadOnlyRepository,
        INotificationCenterUpdateOnlyRepository,
        IPushDeliveryUpdateOnlyRepository
    {
        private readonly MiCashDbContext _dbContext;

        public PushNotificationRepository ( MiCashDbContext dbContext )
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<PushDevice>> GetAllByUserAsync ( long userId , CancellationToken cancellationToken )
        {
            return await _dbContext.PushDevices
                .AsNoTracking()
                .Where(device => device.UserId == userId)
                .OrderByDescending(device => device.IsActive)
                .ThenByDescending(device => device.UpdatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<PushDevice>> GetActiveByUserAsync ( long userId , CancellationToken cancellationToken )
        {
            return await _dbContext.PushDevices
                .AsNoTracking()
                .Where(device => device.UserId == userId && device.IsActive)
                .OrderBy(device => device.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync ( PushDevice device , CancellationToken cancellationToken )
        {
            await _dbContext.PushDevices.AddAsync(device , cancellationToken);
        }

        public Task<PushDevice?> GetByTokenAsync ( string expoPushToken , CancellationToken cancellationToken )
        {
            return _dbContext.PushDevices
                .FirstOrDefaultAsync(device => device.ExpoPushToken == expoPushToken , cancellationToken);
        }

        public async Task<IReadOnlyList<PushDevice>> GetActiveByDeviceIdAsync ( long userId , string deviceId , CancellationToken cancellationToken )
        {
            return await _dbContext.PushDevices
                .Where(device => device.UserId == userId && device.DeviceId == deviceId && device.IsActive)
                .ToListAsync(cancellationToken);
        }

        public Task<bool> ExistsAsync ( long userId , string idempotencyKey , CancellationToken cancellationToken )
        {
            return _dbContext.PushNotifications
                .AnyAsync(n => n.UserId == userId && n.IdempotencyKey == idempotencyKey , cancellationToken);
        }

        public async Task AddAsync ( PushNotification notification , CancellationToken cancellationToken )
        {
            await _dbContext.PushNotifications.AddAsync(notification , cancellationToken);
        }

        public async Task<IReadOnlyList<PushNotification>> GetPageAsync ( long userId , long? before , int take , CancellationToken cancellationToken )
        {
            var query = _dbContext.PushNotifications
                .AsNoTracking()
                .Where(n => n.UserId == userId);

            if ( before is not null ) {
                query = query.Where(n => n.Id < before.Value);
            }

            return await query
                .OrderByDescending(n => n.Id)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public Task<int> CountUnreadAsync ( long userId , CancellationToken cancellationToken )
        {
            return _dbContext.PushNotifications
                .CountAsync(n => n.UserId == userId && n.ReadAt == null , cancellationToken);
        }

        public Task<PushNotification?> GetByIdAsync ( long userId , long id , CancellationToken cancellationToken )
        {
            return _dbContext.PushNotifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId , cancellationToken);
        }

        public async Task<IReadOnlyList<PushNotification>> GetUnreadAsync ( long userId , CancellationToken cancellationToken )
        {
            return await _dbContext.PushNotifications
                .Where(n => n.UserId == userId && n.ReadAt == null)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<PushNotificationDelivery>> GetDueAsync (
            DateTime now , long? notificationId , int take , CancellationToken cancellationToken )
        {
            var query = Deliveries()
                .Where(d => d.Status == PushDeliveryStatus.Pending && d.NextAttemptAt <= now);

            if ( notificationId.HasValue ) {
                query = query.Where(d => d.NotificationId == notificationId.Value);
            }

            return await query
                .OrderBy(d => d.Id)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<PushNotificationDelivery>> GetAwaitingReceiptAsync (
            DateTime sentBefore , int take , CancellationToken cancellationToken )
        {
            return await Deliveries()
                .Where(d => d.Status == PushDeliveryStatus.SentToExpo && d.SentAt <= sentBefore)
                .OrderBy(d => d.ReceiptCheckedAt.HasValue)
                .ThenBy(d => d.ReceiptCheckedAt)
                .ThenBy(d => d.Id)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<PushNotificationDelivery>> GetStuckSendingAsync (
            DateTime claimedBefore , int take , CancellationToken cancellationToken )
        {
            return await Deliveries()
                .Where(d => d.Status == PushDeliveryStatus.Sending && d.ClaimedAt <= claimedBefore)
                .OrderBy(d => d.Id)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        private IQueryable<PushNotificationDelivery> Deliveries () =>
            _dbContext.PushNotificationDeliveries
                .Include(d => d.Notification)
                .Include(d => d.Device);
    }
}
