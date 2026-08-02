using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiCash.Infrastructure.DataAccess.Configurations
{
    public class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
    {
        public void Configure ( EntityTypeBuilder<ConversationMessage> builder )
        {
            builder.ToTable("conversation_messages");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.ConversationId)
                .HasColumnName("conversation_id");

            builder.Property(x => x.Role)
                .HasColumnName("role")
                .HasConversion<string>()
                .IsRequired();

            builder.Property(x => x.Content)
                .HasColumnName("content")
                .HasMaxLength(8000)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnName ("created_at")
                .IsRequired();

            builder.HasIndex(x => x.ConversationId);

            builder.HasIndex(x => new {
                x.ConversationId ,
                x.CreatedAt
            });

            builder.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
