using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.Music;

/// <summary>
/// Request model for updating station music metadata.
/// Fields are partial; only provided values will be changed.
/// </summary>
public sealed class UpdateStationMusicMetadataRequest
{
    [StringLength(300)]
    public string? Title { get; set; }

    [StringLength(200)]
    public string? Artist { get; set; }

    [StringLength(200)]
    public string? Album { get; set; }

    public string? Lyrics { get; set; }
}
