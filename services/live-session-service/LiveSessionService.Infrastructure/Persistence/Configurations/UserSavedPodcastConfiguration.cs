using LiveSessionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveSessionService.Infrastructure.Persistence.Configurations;

public sealed class UserSavedPodcastConfiguration : IEntityTypeConfiguration<UserSavedPodcast>
{
    public void Configure(EntityTypeBuilder<UserSavedPodcast> builder)
    {
        builder.ToTable("user_saved_podcasts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.PodcastId)
            .HasColumnName("podcast_id")
            .IsRequired();

        builder.Property(x => x.SavedAt)
            .HasColumnName("saved_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(x => x.Podcast)
            .WithMany(x => x.UserSavedPodcasts)
            .HasForeignKey(x => x.PodcastId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.PodcastId);
        builder.HasIndex(x => new { x.UserId, x.PodcastId }).IsUnique();
    }
}
