using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.Music;

public sealed class ImportSystemMediaBatchRequest
{
    [Required]
    [MinLength(1)]
    public List<Guid> MediaFileIds { get; set; } = new();
}
