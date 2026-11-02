using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiCash.Infrastructure.DataAccess.Configurations
{
    public sealed class PasswordResetRequestConfiguration : IEntityTypeConfiguration<PasswordResetRequest>
    {
        public void Configure ( EntityTypeBuilder<PasswordResetRequest> builder )
        {
            builder.ToTable("password_reset_request");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.UserId).IsRequired().HasColumnName("user_id");

            builder.Property(x => x.CodeHash).HasMaxLength(100).HasColumnName("code_hash");

            builder.Property(x => x.ExpiresAt).IsRequired().HasColumnName("expires_at");

            builder.Property(x => x.Attempts).IsRequired().HasColumnName("attempts");

            builder.Property(x => x.CodeSentAt).HasColumnName("code_sent_at");

            builder.Property(x => x.VerifiedAt).HasColumnName("verified_at");

            builder.Property(x => x.ResetTokenHash).HasMaxLength(64).HasColumnName("reset_token_hash");

            builder.Property(x => x.ResetTokenExpiresAt).HasColumnName("reset_token_expires_at");

            builder.Property(x => x.CompletedAt).HasColumnName("completed_at");

            builder.Property(x => x.InvalidatedAt).HasColumnName("invalidated_at");

            builder.Property(x => x.CreatedAt).IsRequired().HasColumnName("created_at");

            builder.Ignore(x => x.IsOpen);

            // PostgreSQL xmin: attempts and the single use of code/token are updated with optimistic
            // concurrency, so parallel requests cannot exceed the attempt limit or reuse the token.
            builder.Property<uint>("Version").IsRowVersion();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId , x.CreatedAt })
                .HasDatabaseName("ix_password_reset_request_user_id_created_at");

            builder.HasIndex(x => x.ResetTokenHash)
                .IsUnique()
                .HasFilter("reset_token_hash IS NOT NULL")
                .HasDatabaseName("ux_password_reset_request_reset_token_hash");
        }
    }
}
