using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiCash.Infrastructure.DataAccess.Configurations
{
    internal class PaymentCustomerConfiguration : IEntityTypeConfiguration<PaymentCustomer>
    {
        public void Configure ( EntityTypeBuilder<PaymentCustomer> builder )
        {
            builder.ToTable("payment_customers");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(20).IsRequired();
            builder.Property(x => x.ExternalCustomerId).HasColumnName("external_customer_id").HasMaxLength(64).IsRequired();
            builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150);
            builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
            builder.Property(x => x.FirstUsedAt).HasColumnName("first_used_at").IsRequired();
            builder.Property(x => x.LastUsedAt).HasColumnName("last_used_at").IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

            // Per user: the same gateway customer may pay for several MiCash accounts.
            builder.HasIndex(x => new { x.UserId , x.Provider , x.ExternalCustomerId })
                .IsUnique();

            builder.HasOne(x => x.User)
                .WithMany(u => u.PaymentCustomers)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
