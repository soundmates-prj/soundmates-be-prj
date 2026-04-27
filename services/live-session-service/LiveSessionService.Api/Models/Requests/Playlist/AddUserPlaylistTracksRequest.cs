using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.Playlist;

public sealed class AddUserPlaylistTracksRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one media id is required")]
    public List<Guid> MediaIds { get; set; } = new();
}
