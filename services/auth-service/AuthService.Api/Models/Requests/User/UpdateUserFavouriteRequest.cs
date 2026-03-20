using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests.User;

/// <summary>
/// Request body for updating a favourite item's metadata.
///
/// The business key (ItemType + ItemId + Source) identifies the item.
/// Metadata fields are optional — null means "keep current value".
///
/// Set <c>refreshFromSpotify = true</c> to discard client-provided values
/// and re-fetch real metadata from Spotify (works for Spotify sources only).
/// </summary>
public class UpdateUserFavouriteRequest
{
    // ── Business key — required to identify the favourite ─────────────────────

    [Required]
    [MaxLength(50)]
    public string ItemType { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string ItemId { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Source { get; set; } = null!;

    // ── Metadata — all optional (null = keep existing value) ──────────────────

    [MaxLength(255)]
    public string? Name { get; set; }

    [MaxLength(255)]
    public string? ArtistName { get; set; }

    [MaxLength(255)]
    public string? AlbumName { get; set; }

    [MaxLength(500)]
    public string? ImgUrl { get; set; }

    [MaxLength(500)]
    public string? PreviewUrl { get; set; }

    /// <summary>
    /// When true, metadata is re-fetched from Spotify (source must be "spotify").
    /// Client-provided Name/Artist/etc. are ignored when this is true.
    /// </summary>
    public bool RefreshFromSpotify { get; set; } = false;
}
