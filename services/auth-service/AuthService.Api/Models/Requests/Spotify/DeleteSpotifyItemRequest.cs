using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests.Spotify;

public class DeleteSpotifyItemRequest
{
    [Required]
    [MaxLength(200)]
    public string SpotifyId { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string ItemType { get; set; } = null!;
}
