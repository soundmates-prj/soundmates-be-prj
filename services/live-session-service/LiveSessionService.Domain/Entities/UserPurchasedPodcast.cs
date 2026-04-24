using System;

namespace LiveSessionService.Domain.Entities;

public class UserPurchasedPodcast
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PodcastId { get; set; }
    public decimal Price { get; set; }
    public DateTime PurchasedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual Podcast Podcast { get; set; } = null!;
}
