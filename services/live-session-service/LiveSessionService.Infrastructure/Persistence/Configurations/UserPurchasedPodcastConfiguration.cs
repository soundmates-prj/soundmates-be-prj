using LiveSessionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveSessionService.Infrastructure.Persistence.Configurations;

public class UserPurchasedPodcastConfiguration : IEntityTypeConfiguration<UserPurchasedPodcast>
{
    public void Configure(EntityTypeBuilder<UserPurchasedPodcast> builder)
    {
        builder.ToTable("UserPurchasedPodcasts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.PodcastId)
            .IsRequired();

        builder.Property(x => x.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.PurchasedAt)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasOne(x => x.Podcast)
            .WithMany()
            .HasForeignKey(x => x.PodcastId)
            .OnDelete(DeleteBehavior.Cascade);

        // A user can only purchase a podcast once
        builder.HasIndex(x => new { x.UserId, x.PodcastId }).IsUnique();
    }
}
