using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiCash.Infrastructure.DataAccess.Configurations
{
    public sealed class PaymentLinkConfiguration : IEntityTypeConfiguration<PaymentLink>
    {
        public void Configure ( EntityTypeBuilder<PaymentLink> builder )
        {
            builder.ToTable("payment_link");

            builder.HasKey( x => x.Id );

            builder.Property(x => x.Id )
                .HasColumnName("id")
                .IsRequired();

            builder.Property(x => x.UserId)
                .HasColumnName("userId")
                .IsRequired();

            builder.Property(x => x.PlanId)
                .HasColumnName("plan_id")
                .IsRequired();

            builder.Property(x => x.OrderCode)
                .HasColumnName("order_code")
                .IsRequired();

            builder.Property(x => x.PagarmePaymentLinkId)
                .HasColumnName("pagarme_payment_link_id")
                .IsRequired();

            builder.Property(x => x.CheckoutUrl)
                .HasColumnName("checkout_url")
                .IsRequired ();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at");
                
        }
    }
}
