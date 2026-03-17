using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Entities
{
    public partial class UserPlaylist
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public int ExternalPlaylistId { get; set; }

        public string PlaylistName { get; set; } = null!;

        public PlaylistType Type { get; set; }

        public PlaylistSource Source { get; set; }

        public int PlaylistOrder { get; set; }

        public bool IsEnabled { get; set; } = true;

        public bool IncludeInRequests { get; set; }

        public bool IncludeInOnDemand { get; set; }

        public int Weight { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? LastSyncedAt { get; set; }

        public virtual ICollection<UserPlaylistMedia> UserPlaylistMedias { get; set; } = new List<UserPlaylistMedia>();
    }
}
