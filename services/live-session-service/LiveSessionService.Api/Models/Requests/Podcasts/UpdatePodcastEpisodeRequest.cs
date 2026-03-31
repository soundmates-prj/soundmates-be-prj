using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.Podcasts;

public sealed class UpdatePodcastEpisodeRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(300, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 300 characters")]
    public string Title { get; set; } = null!;

    [StringLength(4000, ErrorMessage = "Description cannot exceed 4000 characters")]
    public string? Description { get; set; }

    [StringLength(1000, MinimumLength = 1, ErrorMessage = "AudioUrl must be between 1 and 1000 characters")]
    public string? AudioUrl { get; set; }

    public IFormFile? AudioFile { get; set; }

    [StringLength(1000, ErrorMessage = "ThumbnailUrl cannot exceed 1000 characters")]
    public string? ThumbnailUrl { get; set; }

    public IFormFile? ThumbnailFile { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "EpisodeNumber must be greater than 0")]
    public int EpisodeNumber { get; set; }

    public DateTime PublishDate { get; set; }
}
