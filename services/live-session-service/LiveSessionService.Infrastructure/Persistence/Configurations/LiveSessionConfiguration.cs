using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Infrastructure.Persistence.Configurations;

public class LiveSessionConfiguration : IEntityTypeConfiguration<LiveSession>
{
    public void Configure(EntityTypeBuilder<LiveSession> builder)
    {
        builder.ToTable("live_sessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.HostUserId)
            .HasColumnName("host_user_id")
            .IsRequired();

        builder.Property(x => x.AzuraCastStationId)
            .HasColumnName("azuracast_station_id");

        builder.Property(x => x.SessionName)
            .HasColumnName("session_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.StartedAt)
            .HasColumnName("started_at");

        builder.Property(x => x.EndedAt)
            .HasColumnName("ended_at");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(x => x.MaxListeners)
            .HasColumnName("max_listeners")
            .HasDefaultValue(100)
            .IsRequired();

        builder.Property(x => x.IsPublic)
            .HasColumnName("is_public")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.ThumbnailUrl)
            .HasColumnName("thumbnail_url")
            .HasMaxLength(500);

        builder.Property(x => x.Genre)
            .HasColumnName("genre")
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(x => x.AzuraCastStation)
            .WithMany(s => s.LiveSessions)
            .HasForeignKey(x => x.AzuraCastStationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Participants)
            .WithOne(p => p.LiveSession)
            .HasForeignKey(p => p.LiveSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Activities)
            .WithOne(a => a.LiveSession)
            .HasForeignKey(a => a.LiveSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.NowPlayingHistory)
            .WithOne(n => n.LiveSession)
            .HasForeignKey(n => n.LiveSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Listeners)
            .WithOne(l => l.LiveSession)
            .HasForeignKey(l => l.LiveSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.SongRequests)
            .WithOne(r => r.LiveSession)
            .HasForeignKey(r => r.LiveSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.SessionSchedules)
            .WithOne(s => s.LiveSession)
            .HasForeignKey(s => s.LiveSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Chats)
            .WithOne(c => c.LiveSession)
            .HasForeignKey(c => c.LiveSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.HostUserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.StartedAt);
        builder.HasIndex(x => x.IsPublic);
    }
}
