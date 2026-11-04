using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure ( EntityTypeBuilder<User> builder )
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.HasIndex(x => x.UserIdentifier)
            .IsUnique();

        builder.HasIndex(x => x.PagarMeCustomerId)
            .IsUnique();

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasColumnName("phone")
            .HasMaxLength(20);

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.UserIdentifier)
            .HasColumnName("user_identifier")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(x => x.PagarMeCustomerId)
            .HasColumnName("pagarme_customer_id");

        builder.Property(x => x.EmailVerifiedAt)
            .HasColumnName("email_verified_at");

        // Defaults for new users come from the entity. No HasDefaultValue: with a bool, EF skips "false" on
        // insert and the database default would win. The migration fills the existing rows (true/true/false).
        builder.Property(x => x.MonthlySummaryEmail)
            .HasColumnName("monthly_summary_email");

        builder.Property(x => x.MonthlySummaryPush)
            .HasColumnName("monthly_summary_push");

        builder.Property(x => x.NewsEmail)
            .HasColumnName("news_email");

        builder.Property(x => x.MonthlySummarySentFor)
            .HasColumnName("monthly_summary_sent_for");

        // Daily cleanup of accounts never confirmed.
        builder.HasIndex(x => x.CreatedAt)
            .HasFilter("email_verified_at IS NULL")
            .HasDatabaseName("ix_users_unverified_created_at");

        //Relationship
        builder.HasMany(x => x.Categories)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Expenses)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Incomes)
            .WithOne(i => i.User)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Subscriptions)
            .WithOne(u => u.User)
            .HasForeignKey (u => u.UserId)
            .OnDelete (DeleteBehavior.Cascade);
    }
}