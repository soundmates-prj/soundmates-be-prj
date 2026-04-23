namespace LiveSessionService.Application.Features.Results.PodcastRequests;

public sealed class PodcastRequestResult
{
    public Guid Id { get; init; }
    public Guid RequestedByUserId { get; init; }
    public object? AuthorInfo { get; init; }
    public string Title { get; init; } = null!;
    public string Type { get; init; } = null!;
    public string? Description { get; init; }
    public string? BannerUrl { get; init; }
    public decimal Price { get; init; }
    public bool IsPaid { get; init; }
    public string Status { get; init; } = null!;
    public Guid? ReviewedByUserId { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectReason { get; init; }
    public DateTime RequestedAt { get; init; }

    // Joined data
    public string? RequestedByUsername { get; init; }
    public string? ReviewedByUsername { get; init; }
}