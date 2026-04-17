using LiveSessionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveSessionService.Infrastructure.Persistence.Configurations;

public sealed class LiveSessionChatConfiguration : IEntityTypeConfiguration<LiveSessionChat>
{
    public void Configure(EntityTypeBuilder<LiveSessionChat> builder)
    {
        builder.ToTable("live_session_chats");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.Message)
            .HasColumnName("message")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.UserName)
            .HasColumnName("user_name")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(x => x.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(x => x.LiveSessionId)
            .HasColumnName("live_session_id")
            .IsRequired();

        builder.HasOne(x => x.LiveSession)
            .WithMany(x => x.Chats)
            .HasForeignKey(x => x.LiveSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LiveSessionId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
