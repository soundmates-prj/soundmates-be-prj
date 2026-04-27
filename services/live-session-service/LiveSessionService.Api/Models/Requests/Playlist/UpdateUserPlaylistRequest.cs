using LiveSessionService.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.Playlist;

public sealed class UpdateUserPlaylistRequest
{
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Playlist name must be between 2 and 200 characters")]
    public string? PlaylistName { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Thumbnail URL cannot exceed 500 characters")]
    public string? ThumbnailUrl { get; set; }

    public PlaylistVisibility? Visibility { get; set; }
    public bool? IsEnabled { get; set; }
}
