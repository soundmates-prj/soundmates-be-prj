using LiveSessionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveSessionService.Infrastructure.Persistence.Configurations;

public sealed class UserPlaylistConfiguration : IEntityTypeConfiguration<UserPlaylist>
{
    public void Configure(EntityTypeBuilder<UserPlaylist> builder)
    {
        builder.ToTable("user_playlists");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.ExternalPlaylistId)
            .HasColumnName("external_playlist_id")
            .IsRequired();

        builder.Property(x => x.PlaylistName)
            .HasColumnName("playlist_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.PlaylistOrder)
            .HasColumnName("playlist_order")
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.IncludeInRequests)
            .HasColumnName("include_in_requests")
            .IsRequired();

        builder.Property(x => x.IncludeInOnDemand)
            .HasColumnName("include_in_on_demand")
            .IsRequired();

        builder.Property(x => x.Weight)
            .HasColumnName("weight")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(x => x.LastSyncedAt)
            .HasColumnName("last_synced_at");

        builder.HasMany(x => x.UserPlaylistMedias)
            .WithOne(x => x.UserPlaylist)
            .HasForeignKey(x => x.UserPlaylistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ExternalPlaylistId);
    }
}
