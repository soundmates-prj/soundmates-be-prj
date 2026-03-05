using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Infrastructure.Persistence.Configurations;

public class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("media_files");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StationId)
            .IsRequired();

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Artist)
            .HasMaxLength(200);

        builder.Property(x => x.Album)
            .HasMaxLength(200);

        builder.Property(x => x.Genre)
            .HasMaxLength(100);

        builder.Property(x => x.ArtUrl)
            .HasMaxLength(500);

        builder.Property(x => x.FilePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.FileType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.UploadedAt)
            .IsRequired();

        // PlaylistMedias relationship defined on PlaylistMedia side
        builder.HasMany(x => x.PlaylistMedias)
            .WithOne(x => x.MediaFile)
            .HasForeignKey(x => x.MediaFileId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
