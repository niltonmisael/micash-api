using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiCash.Infrastructure.DataAccess.Configurations
{
    public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure ( EntityTypeBuilder<Conversation> builder )
        {
            builder.ToTable("conversations");

            builder.HasKey( x => x.Id );

            builder.Property(x => x.Id )
                .HasColumnName("id")
                .IsRequired();

            builder.Property(x => x.UserId )
                .HasColumnName( "userId" );

            builder.Property(x => x.Title)
                .HasMaxLength(200)
                .HasColumnName("title");

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasColumnName ("is_active");

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at");

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");

            //Relationships

            builder.HasOne(x => x.User)
                .WithMany(x => x.Conversations)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Messages)
                .WithOne(x => x.Conversation)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            //Indexes
            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => new {
                x.UserId ,
                x.IsActive
            });
        }
    }
}
