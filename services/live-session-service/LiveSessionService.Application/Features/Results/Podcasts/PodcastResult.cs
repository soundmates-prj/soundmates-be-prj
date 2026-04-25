namespace LiveSessionService.Application.Features.Results.Podcasts;

public sealed class PodcastResult
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public object? Author { get; init; }
    public string Status { get; init; } = null!;
    public string? Type { get; init; }
    public string? Banner { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public Guid CreatedBy { get; init; }
    public decimal Price { get; init; }
    public bool IsPaid { get; init; }
    public int EpisodeCount { get; init; }
    public bool IsPurchased { get; init; }
    public List<PodcastEpisodeResult> AllEpisodes { get; init; } = [];
}

public sealed class PodcastEpisodeResult
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string AudioUrl { get; init; } = null!;
    public string? ThumbnailUrl { get; init; }
    public int EpisodeNumber { get; init; }
    public DateTime PublishDate { get; init; }
    public int Duration { get; init; }
}
