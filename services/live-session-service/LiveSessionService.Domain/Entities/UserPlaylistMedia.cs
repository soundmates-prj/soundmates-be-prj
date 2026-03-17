namespace LiveSessionService.Domain.Entities
{
    public partial class UserPlaylistMedia
    {
        public Guid Id { get; set; }
        public Guid UserPlaylistId { get; set; }
        public Guid? MediaFileId { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual UserPlaylist UserPlaylist { get; set; } = null!;

        public virtual MediaFile? MediaFile { get; set; }
    }
}
