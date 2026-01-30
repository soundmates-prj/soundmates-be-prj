using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Infrastructure.Persistence.Configurations;

public class NowPlayingHistoryConfiguration : IEntityTypeConfiguration<NowPlayingHistory>
{
    public void Configure(EntityTypeBuilder<NowPlayingHistory> builder)
    {
        builder.ToTable("now_playing_history");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.LiveSessionId)
            .HasColumnName("live_session_id")
            .IsRequired();

        builder.Property(x => x.AzuraCastSongHistoryId)
            .HasColumnName("azuracast_song_history_id");

        builder.Property(x => x.SongTitle)
            .HasColumnName("song_title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.SongArtist)
            .HasColumnName("song_artist")
            .HasMaxLength(200);

        builder.Property(x => x.SongAlbum)
            .HasColumnName("song_album")
            .HasMaxLength(200);

        builder.Property(x => x.SongArtUrl)
            .HasColumnName("song_art_url")
            .HasMaxLength(500);

        builder.Property(x => x.DurationSeconds)
            .HasColumnName("duration_seconds")
            .IsRequired();

        builder.Property(x => x.PlayedAt)
            .HasColumnName("played_at")
            .IsRequired();

        builder.Property(x => x.EndedAt)
            .HasColumnName("ended_at");

        builder.Property(x => x.ListenerPeak)
            .HasColumnName("listener_peak")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.ListenerCount)
            .HasColumnName("listener_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.IsRequest)
            .HasColumnName("is_request")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.RequestedByUserId)
            .HasColumnName("requested_by_user_id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.LiveSession)
            .WithMany(s => s.NowPlayingHistory)
            .HasForeignKey(x => x.LiveSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.LiveSessionId);
        builder.HasIndex(x => x.PlayedAt);
        builder.HasIndex(x => new { x.LiveSessionId, x.PlayedAt });
    }
}
