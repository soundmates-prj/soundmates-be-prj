using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Infrastructure.Persistence.Configurations;

public class StationMediaFileConfiguration : IEntityTypeConfiguration<StationMediaFile>
{
    public void Configure(EntityTypeBuilder<StationMediaFile> builder)
    {
        builder.ToTable("station_media_files");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AzuraCastMediaId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ImportedAt)
            .IsRequired();

        // Foreign key to MediaFile
        builder.HasOne(x => x.MediaFile)
            .WithMany(x => x.StationMediaFiles)
            .HasForeignKey(x => x.MediaFileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to AzuraCastStation
        builder.HasOne(x => x.Station)
            .WithMany(x => x.StationMediaFiles)
            .HasForeignKey(x => x.StationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: One MediaFile can only be imported once per Station
        builder.HasIndex(x => new { x.MediaFileId, x.StationId })
            .IsUnique();

        // Index for querying by station
        builder.HasIndex(x => x.StationId);

        // Index for querying by AzuraCastMediaId
        builder.HasIndex(x => x.AzuraCastMediaId);
    }
}
