using LiveSessionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveSessionService.Infrastructure.Persistence.Configurations;

public sealed class SongRequestConfiguration : IEntityTypeConfiguration<SongRequest>
{
    public void Configure(EntityTypeBuilder<SongRequest> builder)
    {
        builder.ToTable("song_requests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.LiveSessionId)
            .HasColumnName("live_session_id")
            .IsRequired();

        builder.Property(x => x.MediaFileId)
            .HasColumnName("media_file_id")
            .IsRequired();

        builder.Property(x => x.RequestedByUserId)
            .HasColumnName("requested_by_user_id")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ReviewedByUserId)
            .HasColumnName("reviewed_by_user_id");

        builder.Property(x => x.RequestedAt)
            .HasColumnName("requested_at")
            .IsRequired();

        builder.Property(x => x.ReviewedAt)
            .HasColumnName("reviewed_at");

        builder.Property(x => x.Message)
            .HasColumnName("message")
            .HasMaxLength(1000);

        builder.Property(x => x.RejectReason)
            .HasColumnName("reject_reason")
            .HasMaxLength(1000);

        builder.HasOne(x => x.LiveSession)
            .WithMany(x => x.SongRequests)
            .HasForeignKey(x => x.LiveSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MediaFile)
            .WithMany()
            .HasForeignKey(x => x.MediaFileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.LiveSessionId);
        builder.HasIndex(x => x.MediaFileId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.RequestedAt);
    }
}
