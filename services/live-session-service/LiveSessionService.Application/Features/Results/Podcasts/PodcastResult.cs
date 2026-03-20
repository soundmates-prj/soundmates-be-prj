namespace LiveSessionService.Application.Features.Results.Podcasts;

public sealed class PodcastResult
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string? Author { get; init; }
    public string Status { get; init; } = null!;
    public string? Type { get; init; }
    public string? Banner { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public Guid CreatedBy { get; init; }
    public int EpisodeCount { get; init; }
}
