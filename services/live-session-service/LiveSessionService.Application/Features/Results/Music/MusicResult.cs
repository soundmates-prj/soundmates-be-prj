using System.Text.Json.Serialization;

namespace LiveSessionService.Application.Features.Results.Music;

/// <summary>
/// Result model for music operations
/// </summary>
public sealed class MusicResult
{
    public Guid Id { get; init; }
    public string SourceType { get; init; } = "system";
    public string Title { get; init; } = null!;
    public string Artist { get; init; } = null!;
    public string? Album { get; init; }
    public string? ArtworkUrl { get; init; }
    public string? Lyrics { get; init; }
    public int Duration { get; init; }
    public string FileUrl { get; init; } = null!;
    public string FileType { get; init; } = null!;
    public long FileSize { get; init; }
    public DateTime UploadedAt { get; init; }
}
