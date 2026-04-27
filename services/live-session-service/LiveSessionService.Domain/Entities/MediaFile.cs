namespace LiveSessionService.Domain.Entities;

/// <summary>
/// Standalone media file storage.
/// Music is uploaded here FIRST, then optionally assigned to a StationPlaylist via PlaylistMedia.
/// </summary>
public class MediaFile
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Artist { get; set; }

    public string? Album { get; set; }

    public string? Genre { get; set; }

    public string? ArtUrl { get; set; }

    public string? Lyrics { get; set; }

    public int DurationSeconds { get; set; }

    /// <summary>
    /// Local storage path token (e.g. system://cloudinary/abc123).
    /// </summary>
    public string FilePath { get; set; } = null!;

    /// <summary>
    /// Actual playable URL — Cloudinary URL for system media, AzuraCast URL for station media.
    /// </summary>
    public string? FileUrl { get; set; }

    /// <summary>
    /// AzuraCast unique_id for synchronized/imported media.
    /// </summary>
    public string? AzuraCastMediaId { get; set; }

    /// <summary>
    /// Original source type: "system" (uploaded to Cloudinary) or "station" (uploaded directly to AzuraCast).
    /// This field preserves the original source even after system media is imported to AzuraCast.
    /// </summary>
    public string OriginalSourceType { get; set; } = "system";

    public string FileType { get; set; } = null!;

    public long FileSizeBytes { get; set; }

    public Guid UploadedByUserId { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<PlaylistMedia> PlaylistMedias { get; set; } = new List<PlaylistMedia>();
    public virtual ICollection<UserPlaylistMedia> UserPlaylistMedias { get; set; } = new List<UserPlaylistMedia>();
    public virtual ICollection<StationMediaFile> StationMediaFiles { get; set; } = new List<StationMediaFile>();
}
