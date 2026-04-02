using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Entities
{
    public partial class UserPlaylist
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public string PlaylistName { get; set; } = null!;

        public string? Description { get; set; }

        public string? ThumbnailUrl { get; set; }

        public PlaylistVisibility Visibility { get; set; }

        public PlaylistType Type { get; set; }

        public int? PlaylistOrder { get; set; }

        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<UserPlaylistMedia> UserPlaylistMedias { get; set; } = new List<UserPlaylistMedia>();
    }
}
