using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests.User;

public class CreateUserFavouriteRequest
{
    [Required]
    [MaxLength(50)]
    public string ItemType { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string ItemId { get; set; } = null!;

    [MaxLength(50)]
    public string Source { get; set; } = "spotify";

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

    public string? RawJson { get; set; }
}
