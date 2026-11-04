using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiCash.Infrastructure.DataAccess.Configurations
{
    internal class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure ( EntityTypeBuilder<Subscription> builder )
        {
            builder.ToTable("subscriptions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(x => x.PlanId)
                .HasColumnName("plan_id")
                .IsRequired();

            builder.Property(x => x.PagarmeSubscriptionId)
                .HasColumnName("pagarme_subscription_id");

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired();

            builder.Property(x => x.StartsAt)
                .HasColumnName("starts_at")
                .IsRequired();

            builder.Property(x => x.EndsAt)
                .HasColumnName("ends_at");

            builder.Property(x => x.PaymentGraceUntil)
                .HasColumnName("payment_grace_until");

            builder.Property(x => x.NextBillingDate)
                .HasColumnName("next_billing_date");

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            builder.Property(x => x.PagarmeCustomerId)
                .HasColumnName("pagarme_customer_id");

            builder.Property(x => x.PagarmeChargeId)
                .HasColumnName("pagarme_charge_id");

            builder.Property(x => x.CancelAtPeriodEnd)
                .HasColumnName("cancel_at_period_end");

            builder.Property(x => x.CancellationRequestedAt)
                .HasColumnName("cancellation_requested_at");

            builder.Property(x => x.CancellationJobId)
                .HasColumnName("cancellation_job_id");

            builder.Property(x => x.CancellationScheduledFor)
                .HasColumnName("cancellation_screduled_for");
                
            // Relationships
            builder.HasOne(x => x.User)
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => x.PlanId);

            builder.HasIndex(x => x.Status);

            builder.HasIndex(x => x.PagarmeSubscriptionId)
                .IsUnique();


        }
    }
}
