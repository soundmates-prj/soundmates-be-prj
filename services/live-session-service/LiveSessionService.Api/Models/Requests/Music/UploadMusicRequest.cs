using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.Music;

/// <summary>
/// Request model for music file upload
/// </summary>
public sealed class UploadMusicRequest
{
    /// <summary>
    /// Optional station context from UI. Media is stored as system media and not auto-added to station.
    /// </summary>
    public Guid? StationId { get; set; }

    /// <summary>Leave empty to auto-detect from file tags.</summary>
    [StringLength(200)]
    public string? Title { get; set; }

    /// <summary>Leave empty to auto-detect from file tags.</summary>
    [StringLength(200)]
    public string? Artist { get; set; }

    /// <summary>Leave empty to auto-detect from file tags.</summary>
    [StringLength(200)]
    public string? Album { get; set; }

    /// <summary>Lyrics string (LRC format or plain text).</summary>
    public string? Lyrics { get; set; }

    [Required(ErrorMessage = "File is required")]
    public IFormFile File { get; set; } = null!;
}
