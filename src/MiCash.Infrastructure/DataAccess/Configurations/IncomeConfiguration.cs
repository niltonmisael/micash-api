using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiCash.Infrastructure.DataAccess.Configurations
{
    internal class IncomeConfiguration : IEntityTypeConfiguration<Income>
    {
        public void Configure ( EntityTypeBuilder<Income> builder )
        {
            builder.ToTable("incomes");

            builder.HasKey( x => x.Id );

            builder.Property(x => x.Title)
                .HasColumnName("title")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Description)
                .HasColumnName("description");

            builder.Property(x => x.Amount)
                .HasColumnName("amount")
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.ReceivedAt)
                .HasColumnName("received_at")
                .IsRequired()
                .HasColumnType("date");

            builder.Property(x => x.Category)
                .HasColumnName("category")
                .IsRequired()
                .HasConversion<string>();

            builder.Property(x => x.IsRecurring)
                .HasColumnName("is_recurring")
                .IsRequired();

            builder.Property(x => x.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Configure the relationship with User
            builder.HasOne(x => x.User)
                .WithMany(u => u.Incomes)
                .HasForeignKey(x => x.UserId)
                .HasConstraintName("fk_income_user")
                .OnDelete(DeleteBehavior.Cascade);

            // Add an index on UserId for faster queries
            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => new
            {
                x.UserId ,
                x.ReceivedAt
            });
        }
    }
}
