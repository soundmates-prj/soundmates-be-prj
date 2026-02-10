using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.Music;

/// <summary>
/// Request model for music file upload
/// </summary>
public sealed class UploadMusicRequest
{
    [Required(ErrorMessage = "Station ID is required")]
    public Guid StationId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Artist is required")]
    [StringLength(200, MinimumLength = 1)]
    public string Artist { get; set; } = null!;

    [StringLength(200)]
    public string? Album { get; set; }

    [Required(ErrorMessage = "File is required")]
    public IFormFile File { get; set; } = null!;
}
