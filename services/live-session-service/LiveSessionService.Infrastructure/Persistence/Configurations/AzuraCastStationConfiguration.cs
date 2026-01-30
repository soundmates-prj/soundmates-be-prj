using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Infrastructure.Persistence.Configurations;

public class AzuraCastStationConfiguration : IEntityTypeConfiguration<AzuraCastStation>
{
    public void Configure(EntityTypeBuilder<AzuraCastStation> builder)
    {
        builder.ToTable("azuracast_stations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.ExternalStationId)
            .HasColumnName("external_station_id")
            .IsRequired();

        builder.Property(x => x.StationName)
            .HasColumnName("station_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.StationShortcode)
            .HasColumnName("station_shortcode")
            .HasMaxLength(50);

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(x => x.StreamUrl)
            .HasColumnName("stream_url")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.PublicPlayerUrl)
            .HasColumnName("public_player_url")
            .HasMaxLength(500);

        builder.Property(x => x.MountPoint)
            .HasColumnName("mount_point")
            .HasMaxLength(100);

        builder.Property(x => x.IsEnabled)
            .HasColumnName("is_enabled")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.ApiBaseUrl)
            .HasColumnName("api_base_url")
            .HasMaxLength(200);

        builder.Property(x => x.ApiKey)
            .HasColumnName("api_key")
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(x => x.LastSyncedAt)
            .HasColumnName("last_synced_at");

        builder.Property(x => x.SyncStatus)
            .HasColumnName("sync_status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.LastSyncError)
            .HasColumnName("last_sync_error")
            .HasMaxLength(1000);

        // Relationships
        builder.HasMany(x => x.LiveSessions)
            .WithOne(s => s.AzuraCastStation)
            .HasForeignKey(s => s.AzuraCastStationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Playlists)
            .WithOne(p => p.AzuraCastStation)
            .HasForeignKey(p => p.AzuraCastStationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Mounts)
            .WithOne(m => m.AzuraCastStation)
            .HasForeignKey(m => m.AzuraCastStationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.ExternalStationId).IsUnique();
        builder.HasIndex(x => x.IsEnabled);
        builder.HasIndex(x => x.SyncStatus);
    }
}
