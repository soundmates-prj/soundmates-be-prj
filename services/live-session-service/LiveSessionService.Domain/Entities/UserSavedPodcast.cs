namespace LiveSessionService.Domain.Entities
{
    public partial class UserSavedPodcast
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid PodcastId { get; set; }

        public DateTime SavedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual Podcast Podcast { get; set; } = null!;
    }
}
