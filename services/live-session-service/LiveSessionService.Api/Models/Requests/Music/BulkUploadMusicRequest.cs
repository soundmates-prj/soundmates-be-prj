namespace LiveSessionService.Api.Models.Requests.Music;

/// <summary>
/// Request model for bulk music file upload
/// Supports multiple files in a single request
/// </summary>
public sealed class BulkUploadMusicRequest
{
    /// <summary>
    /// Optional station context from UI. Media is stored as system media.
    /// </summary>
    public Guid? StationId { get; set; }

    /// <summary>
    /// Bulk music files to upload. Supported: MP3, FLAC, WAV, OGG.
    /// Max total size: 500MB per request.
    /// Max individual file size: 100MB.
    /// </summary>
    public List<IFormFile> Files { get; set; } = new();
}
