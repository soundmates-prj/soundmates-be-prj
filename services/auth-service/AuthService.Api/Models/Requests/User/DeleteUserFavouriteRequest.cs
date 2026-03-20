using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests.User;

public class DeleteUserFavouriteRequest
{
    [Required]
    [MaxLength(50)]
    public string ItemType { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string ItemId { get; set; } = null!;

    [MaxLength(50)]
    public string Source { get; set; } = "spotify";
}
