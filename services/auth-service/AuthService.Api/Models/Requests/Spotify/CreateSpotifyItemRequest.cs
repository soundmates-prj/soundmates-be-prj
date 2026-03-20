using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests.Spotify;

public class CreateSpotifyItemRequest
{
    [Required]
    [MaxLength(200)]
    public string SpotifyId { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string ItemType { get; set; } = null!;

    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string ArtistName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string AlbumName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ImgUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string PreviewUrl { get; set; } = string.Empty;

    public string RawJson { get; set; } = string.Empty;
}
