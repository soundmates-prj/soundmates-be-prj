using LiveSessionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveSessionService.Infrastructure.Persistence.Configurations;

public sealed class PodcastEpisodeConfiguration : IEntityTypeConfiguration<PodcastEpisode>
{
    public void Configure(EntityTypeBuilder<PodcastEpisode> builder)
    {
        builder.ToTable("podcast_episodes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(4000);

        builder.Property(x => x.AudioUrl)
            .HasColumnName("audio_url")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.EpisodeNumber)
            .HasColumnName("episode_number")
            .IsRequired();

        builder.Property(x => x.PublishDate)
            .HasColumnName("publish_date")
            .IsRequired();

        builder.Property(x => x.Duration)
            .HasColumnName("duration")
            .IsRequired();

        builder.Property(x => x.PodcastId)
            .HasColumnName("podcast_id")
            .IsRequired();

        builder.HasOne(x => x.Podcast)
            .WithMany(x => x.Episodes)
            .HasForeignKey(x => x.PodcastId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.PodcastId);
        builder.HasIndex(x => new { x.PodcastId, x.EpisodeNumber }).IsUnique();
    }
}
