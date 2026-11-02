using MiCash.Domain.Enums;

namespace MiCash.Domain.Entities
{
    /// <summary>A device (app install) that receives push notifications through the Expo Push Service.</summary>
    public class PushDevice
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public User User { get; set; } = default!;

        /// <summary>ExponentPushToken[...]. Unique: one install belongs to one user at a time.</summary>
        public string ExpoPushToken { get; set; } = string.Empty;

        public DevicePlatform Platform { get; set; }

        public string? DeviceId { get; set; }

        public string? DeviceName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Last time Expo accepted a notification for this device.</summary>
        public DateTime? LastUsedAt { get; set; }
    }
}
