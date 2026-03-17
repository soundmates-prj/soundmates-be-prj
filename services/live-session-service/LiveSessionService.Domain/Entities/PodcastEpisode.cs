namespace LiveSessionService.Domain.Entities
{
    public partial class PodcastEpisode
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public string AudioUrl { get; set; } = null!;

        public int EpisodeNumber { get; set; }

        public DateTime PublishDate { get; set; }

        public int Duration { get; set; }

        public Guid PodcastId { get; set; }

        public virtual Podcast Podcast { get; set; } = null!;
    }
}
