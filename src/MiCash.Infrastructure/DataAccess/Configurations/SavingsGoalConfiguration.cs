using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiCash.Infrastructure.DataAccess.Configurations
{
    public sealed class SavingsGoalConfiguration : IEntityTypeConfiguration<SavingsGoal>
    {
        public const int NAME_MAX_LENGTH = 100;

        public void Configure ( EntityTypeBuilder<SavingsGoal> builder )
        {
            builder.ToTable("savings_goal");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.UserId).IsRequired().HasColumnName("user_id");

            builder.Property(x => x.Name).IsRequired().HasMaxLength(NAME_MAX_LENGTH).HasColumnName("name");

            builder.Property(x => x.TargetAmount).IsRequired().HasPrecision(18 , 2).HasColumnName("target_amount");

            builder.Property(x => x.Deadline).HasColumnName("deadline");

            builder.Property(x => x.Icon).IsRequired().HasMaxLength(50).HasColumnName("icon");

            builder.Property(x => x.Color).IsRequired().HasMaxLength(20).HasColumnName("color");

            builder.Property(x => x.CreatedAt).IsRequired().HasColumnName("created_at");

            builder.Property(x => x.UpdatedAt).IsRequired().HasColumnName("updated_at");

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Movements)
                .WithOne(x => x.SavingsGoal)
                .HasForeignKey(x => x.SavingsGoalId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId);

            // Optimistic concurrency on Postgres' system column xmin (no real column is added).
            // Every movement also updates the goal row, so two concurrent withdrawals cannot both
            // pass the balance check: the second UPDATE ... WHERE xmin = @read fails and is retried.
            builder.Property<uint>(CONCURRENCY_TOKEN)
                .IsRowVersion();
        }

        public const string CONCURRENCY_TOKEN = "Version";
    }

    public sealed class SavingsGoalMovementConfiguration : IEntityTypeConfiguration<SavingsGoalMovement>
    {
        public void Configure ( EntityTypeBuilder<SavingsGoalMovement> builder )
        {
            builder.ToTable("savings_goal_movement");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.SavingsGoalId).IsRequired().HasColumnName("savings_goal_id");

            builder.Property(x => x.Type).IsRequired().HasColumnName("type");

            builder.Property(x => x.Amount).IsRequired().HasPrecision(18 , 2).HasColumnName("amount");

            builder.Property(x => x.Date).IsRequired().HasColumnName("date");

            builder.Property(x => x.CreatedAt).IsRequired().HasColumnName("created_at");

            builder.HasIndex(x => x.SavingsGoalId);
        }
    }
}
