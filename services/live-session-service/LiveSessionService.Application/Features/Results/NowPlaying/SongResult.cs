namespace LiveSessionService.Application.Features.Results.NowPlaying;

/// <summary>
/// Result for Song information
/// </summary>
public sealed class SongResult
{
    public string Title { get; init; } = null!;
    public string? Artist { get; init; }
    public string? Album { get; init; }
    public string? ArtUrl { get; init; }
    public int DurationSeconds { get; init; }
    public bool IsRequest { get; init; }
    public Guid? RequestedByUserId { get; init; }
}
