using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiCash.Infrastructure.DataAccess.Configurations
{
    public sealed class PushDeviceConfiguration : IEntityTypeConfiguration<PushDevice>
    {
        public const int TOKEN_MAX_LENGTH = 255;
        public const int DEVICE_ID_MAX_LENGTH = 200;
        public const int DEVICE_NAME_MAX_LENGTH = 100;

        public void Configure ( EntityTypeBuilder<PushDevice> builder )
        {
            builder.ToTable("push_device");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.UserId).IsRequired().HasColumnName("user_id");

            builder.Property(x => x.ExpoPushToken).IsRequired().HasMaxLength(TOKEN_MAX_LENGTH).HasColumnName("expo_push_token");

            builder.Property(x => x.Platform).IsRequired().HasMaxLength(20).HasColumnName("platform");

            builder.Property(x => x.DeviceId).HasMaxLength(DEVICE_ID_MAX_LENGTH).HasColumnName("device_id");

            builder.Property(x => x.DeviceName).HasMaxLength(DEVICE_NAME_MAX_LENGTH).HasColumnName("device_name");

            builder.Property(x => x.IsActive).IsRequired().HasColumnName("is_active");

            builder.Property(x => x.CreatedAt).IsRequired().HasColumnName("created_at");

            builder.Property(x => x.UpdatedAt).IsRequired().HasColumnName("updated_at");

            builder.Property(x => x.LastUsedAt).HasColumnName("last_used_at");

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One install (token) belongs to a single user: registering it again updates the row.
            builder.HasIndex(x => x.ExpoPushToken)
                .IsUnique()
                .HasDatabaseName("ux_push_device_expo_push_token");

            builder.HasIndex(x => new { x.UserId , x.IsActive })
                .HasDatabaseName("ix_push_device_user_id_is_active");
        }
    }

    public sealed class PushNotificationConfiguration : IEntityTypeConfiguration<PushNotification>
    {
        public const int TYPE_MAX_LENGTH = 50;
        public const int TITLE_MAX_LENGTH = 150;
        public const int BODY_MAX_LENGTH = 1000;
        public const int IDEMPOTENCY_KEY_MAX_LENGTH = 200;

        public void Configure ( EntityTypeBuilder<PushNotification> builder )
        {
            builder.ToTable("push_notification");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.UserId).IsRequired().HasColumnName("user_id");

            builder.Property(x => x.Type).IsRequired().HasMaxLength(TYPE_MAX_LENGTH).HasColumnName("type");

            builder.Property(x => x.Title).IsRequired().HasMaxLength(TITLE_MAX_LENGTH).HasColumnName("title");

            builder.Property(x => x.Body).IsRequired().HasMaxLength(BODY_MAX_LENGTH).HasColumnName("body");

            builder.Property(x => x.Data).IsRequired().HasColumnType("jsonb").HasColumnName("data");

            builder.Property(x => x.IdempotencyKey).HasMaxLength(IDEMPOTENCY_KEY_MAX_LENGTH).HasColumnName("idempotency_key");

            builder.Property(x => x.CreatedAt).IsRequired().HasColumnName("created_at");

            builder.Property(x => x.ReadAt).HasColumnName("read_at");

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unread counter of the bell: only unread rows are indexed.
            builder.HasIndex(x => x.UserId)
                .HasFilter("\"read_at\" IS NULL")
                .HasDatabaseName("ix_push_notification_user_id_unread");

            builder.HasMany(x => x.Deliveries)
                .WithOne(x => x.Notification)
                .HasForeignKey(x => x.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId , x.CreatedAt })
                .HasDatabaseName("ix_push_notification_user_id_created_at");

            // The same business event never produces two notifications for the same user.
            builder.HasIndex(x => new { x.UserId , x.IdempotencyKey })
                .IsUnique()
                .HasFilter("\"idempotency_key\" IS NOT NULL")
                .HasDatabaseName("ux_push_notification_user_id_idempotency_key");
        }
    }

    public sealed class PushNotificationDeliveryConfiguration : IEntityTypeConfiguration<PushNotificationDelivery>
    {
        public const string CONCURRENCY_TOKEN = "Version";
        public const int ERROR_CODE_MAX_LENGTH = 100;
        public const int ERROR_MESSAGE_MAX_LENGTH = 500;

        public void Configure ( EntityTypeBuilder<PushNotificationDelivery> builder )
        {
            builder.ToTable("push_notification_delivery");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.NotificationId).IsRequired().HasColumnName("notification_id");

            builder.Property(x => x.DeviceId).IsRequired().HasColumnName("device_id");

            builder.Property(x => x.Status).IsRequired().HasMaxLength(20).HasColumnName("status");

            builder.Property(x => x.Attempts).IsRequired().HasColumnName("attempts");

            builder.Property(x => x.NextAttemptAt).IsRequired().HasColumnName("next_attempt_at");

            builder.Property(x => x.ClaimedAt).HasColumnName("claimed_at");

            builder.Property(x => x.ExpoTicketId).HasMaxLength(100).HasColumnName("expo_ticket_id");

            builder.Property(x => x.ErrorCode).HasMaxLength(ERROR_CODE_MAX_LENGTH).HasColumnName("error_code");

            builder.Property(x => x.ErrorMessage).HasMaxLength(ERROR_MESSAGE_MAX_LENGTH).HasColumnName("error_message");

            builder.Property(x => x.SentAt).HasColumnName("sent_at");

            builder.Property(x => x.ReceiptCheckedAt).HasColumnName("receipt_checked_at");

            builder.Property(x => x.CreatedAt).IsRequired().HasColumnName("created_at");

            builder.Property(x => x.UpdatedAt).IsRequired().HasColumnName("updated_at");

            builder.HasOne(x => x.Device)
                .WithMany()
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            // A notification goes to each device once.
            builder.HasIndex(x => new { x.NotificationId , x.DeviceId })
                .IsUnique()
                .HasDatabaseName("ux_push_notification_delivery_notification_device");

            builder.HasIndex(x => x.ExpoTicketId)
                .IsUnique()
                .HasFilter("\"expo_ticket_id\" IS NOT NULL")
                .HasDatabaseName("ux_push_notification_delivery_expo_ticket_id");

            // Job queries: due Pending, Sending stuck, SentToExpo awaiting receipt.
            builder.HasIndex(x => new { x.Status , x.NextAttemptAt })
                .HasDatabaseName("ix_push_notification_delivery_status_next_attempt_at");

            builder.HasIndex(x => x.DeviceId);

            // Optimistic concurrency on xmin: two jobs can't both claim (and send) the same delivery.
            builder.Property<uint>(CONCURRENCY_TOKEN)
                .IsRowVersion();
        }
    }
}
