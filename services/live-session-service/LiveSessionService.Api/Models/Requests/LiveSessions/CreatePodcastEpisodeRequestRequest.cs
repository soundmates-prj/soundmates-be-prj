using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.LiveSessions;

public sealed class CreatePodcastEpisodeRequestRequest
{
    [Required(ErrorMessage = "PodcastId is required")]
    public Guid PodcastId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
    public string Title { get; set; } = null!;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    public string? ThumbnailUrl { get; set; }

    [Required(ErrorMessage = "Audio URL is required")]
    public string AudioUrl { get; set; } = null!;

    public int Duration { get; set; }
}
