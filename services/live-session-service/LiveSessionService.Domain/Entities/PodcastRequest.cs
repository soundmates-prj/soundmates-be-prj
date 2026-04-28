using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Entities;

public class PodcastRequest
{
    public Guid Id { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? TargetPodcastId { get; set; }
    
    // New fields
    public string? AuthorInfo { get; set; }
    public string Title { get; set; } = null!; // Podcast Series Title
    public string Type { get; set; } = null!;
    public string? Description { get; set; }
    public string? BannerUrl { get; set; }
    public decimal Price { get; set; }
    public bool IsPaid { get; set; }
    public PodcastRequestStatus Status { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectReason { get; set; }
    public DateTime RequestedAt { get; set; }
}