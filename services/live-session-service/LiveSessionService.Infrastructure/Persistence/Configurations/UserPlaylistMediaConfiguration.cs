using LiveSessionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveSessionService.Infrastructure.Persistence.Configurations;

public sealed class UserPlaylistMediaConfiguration : IEntityTypeConfiguration<UserPlaylistMedia>
{
    public void Configure(EntityTypeBuilder<UserPlaylistMedia> builder)
    {
        builder.ToTable("user_playlist_medias");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.UserPlaylistId)
            .HasColumnName("user_playlist_id")
            .IsRequired();

        builder.Property(x => x.MediaFileId)
            .HasColumnName("media_file_id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne(x => x.UserPlaylist)
            .WithMany(x => x.UserPlaylistMedias)
            .HasForeignKey(x => x.UserPlaylistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MediaFile)
            .WithMany(x => x.UserPlaylistMedias)
            .HasForeignKey(x => x.MediaFileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.UserPlaylistId);
        builder.HasIndex(x => x.MediaFileId);
    }
}
