using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.LiveSessions;

public sealed class CreatePodcastRequestRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Type is required")]
    [StringLength(50, ErrorMessage = "Type cannot exceed 50 characters")]
    public string Type { get; set; } = null!;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    public string? BannerUrl { get; set; }

    public decimal? Price { get; set; }

    public bool? IsPaid { get; set; }

    public Guid? TargetPodcastId { get; set; }
}