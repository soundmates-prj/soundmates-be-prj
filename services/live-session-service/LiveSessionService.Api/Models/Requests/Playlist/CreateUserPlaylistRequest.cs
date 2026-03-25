using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.Playlist;

public sealed class CreateUserPlaylistRequest
{
    [Required(ErrorMessage = "Playlist name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Playlist name must be between 2 and 200 characters")]
    public string PlaylistName { get; set; } = null!;

    public bool IncludeInRequests { get; set; }
    public bool IncludeInOnDemand { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int PlaylistOrder { get; set; }
    public int Weight { get; set; } = 1;
}
