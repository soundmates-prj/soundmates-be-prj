namespace LiveSessionService.Api.Models.Requests.Playlist;

public sealed class UpdatePlaylistRequest
{
    public string? PlaylistName { get; set; }
    public string? Description { get; set; }
    public bool? IsAutoPlay { get; set; }
    public bool? IncludeInRequests { get; set; }
    public bool? IncludeInOnDemand { get; set; }
    public bool? IsEnabled { get; set; }
    public string? SongPlaybackOrder { get; set; }
}
