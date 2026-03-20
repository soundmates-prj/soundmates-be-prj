namespace LiveSessionService.Api.Models.Requests.Playlist
{
    public sealed class CreatePlaylistRequest
    {
        public Guid StationId { get; set; }
        public string PlaylistName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsAutoPlay { get; set; }
        public bool IncludeInRequests { get; set; } = true;
    }
}
