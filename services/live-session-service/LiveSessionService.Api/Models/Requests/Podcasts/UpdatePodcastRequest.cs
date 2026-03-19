using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.Podcasts;

public sealed class UpdatePodcastRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(300, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 300 characters")]
    public string Title { get; set; } = null!;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    [StringLength(200, ErrorMessage = "Author cannot exceed 200 characters")]
    public string? Author { get; set; }

    [StringLength(100, ErrorMessage = "Type cannot exceed 100 characters")]
    public string? Type { get; set; }

    [StringLength(1000, ErrorMessage = "Banner cannot exceed 1000 characters")]
    public string? Banner { get; set; }

    public string? Status { get; set; }
}
