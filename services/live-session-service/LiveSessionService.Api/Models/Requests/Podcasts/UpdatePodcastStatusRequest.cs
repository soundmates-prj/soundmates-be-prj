using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.Podcasts;

public sealed class UpdatePodcastStatusRequest
{
    [Required(ErrorMessage = "Status is required")]
    [RegularExpression("(?i)^(public|private|draft|pendingreview|published|archived)$",
        ErrorMessage = "Status must be one of: public, private, draft, pendingreview, published, archived")]
    public string Status { get; set; } = null!;
}
