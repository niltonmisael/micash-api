using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiCash.Infrastructure.DataAccess.Configurations
{
    internal class PlanConfiguration : IEntityTypeConfiguration<Plan>
    {
        public void Configure ( EntityTypeBuilder<Plan> builder )
        {
            builder.ToTable("plans");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .HasColumnName("id");

            builder.Property(p => p.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.Price)
                .HasColumnName("price")
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            // Stored as text like every enum (Monthly / Yearly).
            builder.Property(p => p.BillingPeriod)
                .HasColumnName("billing_period")
                .IsRequired();

            builder.Property(p => p.IsActive)
                .HasColumnName("is_active")
                .IsRequired();

            builder.Property(p => p.PagarmePlanId)
                .HasColumnName("pagarme_plan_id")
                .HasMaxLength(100);

            // Configure the relationship with Subscription
            builder.HasMany(p => p.Subscriptions)
                .WithOne(s => s.Plan)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            //Indexes
            builder.HasIndex(x => x.PagarmePlanId)
                .IsUnique();
        }
    }
}
