using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiCash.Infrastructure.DataAccess.Configurations
{
    public sealed class FinancialGoalConfiguration : IEntityTypeConfiguration<FinancialGoal>
    {
        public void Configure ( EntityTypeBuilder<FinancialGoal> builder )
        {
            builder.ToTable("financial_goal");

            builder.HasKey( x => x.Id );

            builder.Property( x => x.Id )
                .HasColumnName( "id" );

            builder.Property( x => x.UserId )
                .IsRequired()
                .HasColumnName ( "user_id" );

            builder.Property(x => x.CategoryId)
                .IsRequired()
                .HasColumnName("category_id");

            builder.Property(x => x.TargetAmount)
                .IsRequired()
                .HasColumnName("target_amount");

            builder.Property(x => x.Month)
                .IsRequired()
                .HasColumnName("month");

            builder.Property(x => x.Year)
                .IsRequired()
                .HasColumnName("year");

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");
                

            // Relationships
            builder.HasOne(x => x.User)
                .WithMany(u => u.FinancialGoals)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Category)
                .WithMany(c => c.FinancialGoals)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => x.CategoryId);

            builder.HasIndex(x => new { x.UserId, x.Month, x.Year });

            builder.HasIndex(x => new
            {
                x.UserId ,
                x.CategoryId ,
                x.Month ,
                x.Year
            })
.IsUnique();


        }
    }
}
