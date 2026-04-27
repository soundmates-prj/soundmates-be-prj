using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Entities;

public class PodcastEpisodeRequest
{
    public Guid Id { get; set; }
    public Guid PodcastId { get; set; } // The target podcast series
    public Guid RequestedByUserId { get; set; }
    public string? AuthorInfo { get; set; }
    
    // Episode Details
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string AudioUrl { get; set; } = null!;
    public int Duration { get; set; }
    
    public PodcastRequestStatus Status { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectReason { get; set; }
    public DateTime RequestedAt { get; set; }
}
