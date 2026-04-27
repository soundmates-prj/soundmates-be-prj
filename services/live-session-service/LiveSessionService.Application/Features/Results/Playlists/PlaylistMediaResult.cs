namespace LiveSessionService.Application.Features.Results.Playlists;

public sealed class PlaylistMediaResult
{
    public Guid Id { get; init; }
    public Guid PlaylistId { get; init; }
    public Guid MediaFileId { get; init; }
    public string Title { get; init; } = null!;
    public string? Artist { get; init; }
    public string? Album { get; init; }
    public string? ArtworkUrl { get; init; }
    public string? FileUrl { get; init; }
    public string? FileType { get; init; }
    public long? FileSize { get; init; }
    public int DurationSeconds { get; init; }
    public DateTime AddedAt { get; init; }
}
