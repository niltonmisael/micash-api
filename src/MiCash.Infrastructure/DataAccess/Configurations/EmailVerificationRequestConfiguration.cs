using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiCash.Infrastructure.DataAccess.Configurations
{
    public sealed class EmailVerificationRequestConfiguration : IEntityTypeConfiguration<EmailVerificationRequest>
    {
        public void Configure ( EntityTypeBuilder<EmailVerificationRequest> builder )
        {
            builder.ToTable("email_verification_request");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.UserId).IsRequired().HasColumnName("user_id");

            builder.Property(x => x.Purpose).IsRequired().HasColumnName("purpose");

            builder.Property(x => x.Email).IsRequired().HasMaxLength(255).HasColumnName("email");

            builder.Property(x => x.CodeHash).HasMaxLength(100).HasColumnName("code_hash");

            builder.Property(x => x.ExpiresAt).IsRequired().HasColumnName("expires_at");

            builder.Property(x => x.Attempts).IsRequired().HasColumnName("attempts");

            builder.Property(x => x.CodeSentAt).HasColumnName("code_sent_at");

            builder.Property(x => x.VerifiedAt).HasColumnName("verified_at");

            builder.Property(x => x.InvalidatedAt).HasColumnName("invalidated_at");

            builder.Property(x => x.CreatedAt).IsRequired().HasColumnName("created_at");

            builder.Ignore(x => x.IsOpen);

            // PostgreSQL xmin: the attempt counter and the single use of the code are updated with optimistic
            // concurrency, so parallel requests cannot exceed the attempt limit.
            builder.Property<uint>("Version").IsRowVersion();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId , x.CreatedAt })
                .HasDatabaseName("ix_email_verification_request_user_id_created_at");
        }
    }
}
