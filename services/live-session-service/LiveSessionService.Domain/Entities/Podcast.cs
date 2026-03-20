using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Entities
{
    public partial class Podcast
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public string? Author { get; set; }

        public PodcastStatus Status { get; set; }

        public string? Type { get; set; }

        public string? Banner { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public Guid CreatedBy { get; set; }

        public virtual ICollection<PodcastEpisode> Episodes { get; set; } = new List<PodcastEpisode>();
    }
}
