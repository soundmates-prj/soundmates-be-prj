namespace LiveSessionService.Application.Features.Common.AzuraCast.Models;

/// <summary>
/// Represents a media file in an AzuraCast station
/// Maps to the response from GET /api/station/{station_id}/files
/// </summary>
public sealed class AzuraCastStationFileData
{
    public int Id { get; init; }
    public string UniqueId { get; init; } = null!;
    public string? SongId { get; init; }
    public string? Text { get; init; }
    public string? Artist { get; init; }
    public string? Title { get; init; }
    public string? Album { get; init; }
    public string? Genre { get; init; }
    public string? Isrc { get; init; }
    public string? Lyrics { get; init; }
    public string? Art { get; init; }
    public string Path { get; init; } = null!;
    public long Mtime { get; init; }
    public long UploadedAt { get; init; }
    public long ArtUpdatedAt { get; init; }
    public double Length { get; init; }
    public string? LengthText { get; init; }
    public List<AzuraCastFilePlaylistInfo>? Playlists { get; init; }
}

public sealed class AzuraCastFilePlaylistInfo
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public string? ShortName { get; init; }
    public string? Folder { get; init; }
    public int Count { get; init; }
}
