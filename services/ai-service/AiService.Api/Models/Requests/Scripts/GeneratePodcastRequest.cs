using System.ComponentModel.DataAnnotations;

namespace AiService.Api.Models.Requests.Scripts;

public class GeneratePodcastRequest
{
    [Required]
    public string Topic { get; set; } = default!;

    public string? Title { get; set; }

    public string ContextType { get; set; } = "podcast";

    public string? ModelName { get; set; }

    public decimal? Temperature { get; set; }

    public int? MaxTokens { get; set; }
}

