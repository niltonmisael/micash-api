using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiCash.Infrastructure.DataAccess.Configurations
{
    public class ProcessedWebhookConfiguration : IEntityTypeConfiguration<ProcessedWebhook>
    {
        public void Configure ( EntityTypeBuilder<ProcessedWebhook> builder )
        {
            builder.ToTable("processed_webhook");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.WebhookId)
                .HasColumnName("webhook_id");

            builder.Property(x => x.EventType)
                .HasColumnName("event_type");

            builder.Property(x => x.Payload)
                .HasColumnName ("payload");

            builder.Property(x => x.Success)
                .HasColumnName("success");

            builder.Property(x => x.Error)
                .HasColumnName("error");

            builder.Property(x => x.ProcessedAt)
                .HasColumnName("processed_at");

            //Indexes
            // One row per Pagar.me event: retries update it; concurrent duplicate deliveries hit the constraint.
            builder.HasIndex(x => x.WebhookId)
                .IsUnique();
        }
    }
}
