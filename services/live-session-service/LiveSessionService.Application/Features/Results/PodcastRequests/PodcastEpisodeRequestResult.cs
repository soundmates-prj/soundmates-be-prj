namespace LiveSessionService.Application.Features.Results.PodcastRequests;

public sealed class PodcastEpisodeRequestResult
{
    public Guid Id { get; init; }
    public Guid PodcastId { get; init; }
    public Guid RequestedByUserId { get; init; }
    public object? AuthorInfo { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string AudioUrl { get; init; } = null!;
    public int Duration { get; init; }
    
    public string Status { get; init; } = null!;
    public Guid? ReviewedByUserId { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectReason { get; init; }
    public DateTime RequestedAt { get; init; }
}
