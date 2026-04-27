using System.ComponentModel.DataAnnotations;

namespace AiService.Api.Models.Requests.Scripts;

/// <summary>
/// Request body for generating a podcast script.
/// </summary>
public class GeneratePodcastRequest
{
    /// <summary>
    /// Main topic to generate content for.
    /// </summary>
    [Required]
    public string Topic { get; set; } = default!;

    /// <summary>
    /// Optional title for the generated podcast script.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Script context type. Default is <c>podcast</c>.
    /// </summary>
    public string ContextType { get; set; } = "podcast";

    /// <summary>
    /// Optional model name (for example: <c>gemini-2.5-flash</c>).
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// Creativity level. Typical range: 0.2 to 1.0.
    /// </summary>
    public decimal? Temperature { get; set; }

    /// <summary>
    /// Maximum output tokens allowed for each model call.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Optional editor instructions to customize tone, style, and structure.
    /// </summary>
    public string? EditorInstruction { get; set; }

    /// <summary>
    /// If <c>true</c>, combine system auto-context with user/editor instruction.
    /// If <c>false</c>, prioritize user/editor instruction only.
    /// </summary>
    public bool UseAutoContext { get; set; } = true;

    /// <summary>
    /// If <c>true</c>, enforce more conservative fact writing (avoid unsupported claims).
    /// </summary>
    public bool StrictFactMode { get; set; } = false;
}

