using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.Playlist;

public sealed class UpdateUserPlaylistRequest
{
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Playlist name must be between 2 and 200 characters")]
    public string? PlaylistName { get; set; }

    public bool? IncludeInRequests { get; set; }
    public bool? IncludeInOnDemand { get; set; }
    public bool? IsEnabled { get; set; }
    public int? PlaylistOrder { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Weight cannot be negative")]
    public int? Weight { get; set; }
}
