using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiCash.Infrastructure.DataAccess.Configurations
{
    internal class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
    {
        public void Configure ( EntityTypeBuilder<PaymentTransaction> builder )
        {
            builder.ToTable("payment_transactions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.SubscriptionId).HasColumnName("subscription_id").IsRequired();
            builder.Property(x => x.ExternalChargeId).HasColumnName("external_charge_id").HasMaxLength(64).IsRequired();
            builder.Property(x => x.AmountCents).HasColumnName("amount_cents");
            builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.PaidAt).HasColumnName("paid_at");
            builder.Property(x => x.FailedAt).HasColumnName("failed_at");
            builder.Property(x => x.RefundedAt).HasColumnName("refunded_at");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

            builder.HasIndex(x => x.ExternalChargeId).IsUnique();
            builder.HasIndex(x => x.SubscriptionId);

            builder.HasOne(x => x.Subscription)
                .WithMany(s => s.Transactions)
                .HasForeignKey(x => x.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
