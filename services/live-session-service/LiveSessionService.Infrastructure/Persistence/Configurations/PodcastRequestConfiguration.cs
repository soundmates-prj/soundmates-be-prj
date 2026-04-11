using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveSessionService.Infrastructure.Persistence.Configurations;

public sealed class PodcastRequestConfiguration : IEntityTypeConfiguration<PodcastRequest>
{
    public void Configure(EntityTypeBuilder<PodcastRequest> builder)
    {
        builder.ToTable("podcast_requests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.LiveSessionId).HasColumnName("live_session_id").IsRequired();
        builder.Property(r => r.RequestedByUserId).HasColumnName("requested_by_user_id").IsRequired();
        builder.Property(r => r.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        builder.Property(r => r.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(r => r.ScriptText).HasColumnName("script_text").IsRequired();
        builder.Property(r => r.AudioUrl).HasColumnName("audio_url").HasMaxLength(2000).IsRequired();
        builder.Property(r => r.DurationSeconds).HasColumnName("duration_seconds");
        builder.Property(r => r.VoiceCode).HasColumnName("voice_code").HasMaxLength(200).IsRequired();
        builder.Property(r => r.VoiceDisplayName).HasColumnName("voice_display_name").HasMaxLength(200);
        builder.Property(r => r.AzuraCastMediaId).HasColumnName("azuracast_media_id").HasMaxLength(200);
        builder.Property(r => r.Status).HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(r => r.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
        builder.Property(r => r.ReviewedAt).HasColumnName("reviewed_at");
        builder.Property(r => r.RejectReason).HasColumnName("reject_reason").HasMaxLength(1000);
        builder.Property(r => r.RequestedAt).HasColumnName("requested_at").IsRequired();

        builder.HasOne(r => r.LiveSession)
            .WithMany(s => s.PodcastRequests)
            .HasForeignKey(r => r.LiveSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.LiveSessionId).HasDatabaseName("ix_podcast_requests_live_session_id");
        builder.HasIndex(r => r.RequestedByUserId).HasDatabaseName("ix_podcast_requests_requested_by_user_id");
        builder.HasIndex(r => r.Status).HasDatabaseName("ix_podcast_requests_status");
        builder.HasIndex(r => r.RequestedAt).HasDatabaseName("ix_podcast_requests_requested_at");
    }
}