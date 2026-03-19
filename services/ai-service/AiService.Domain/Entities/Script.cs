using AiService.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AiService.Domain.Entities;

public class Script
{
    [Key]
    public Guid ScriptId { get; set; }

    public Guid AuthorId { get; set; }

    [MaxLength(50)]
    public string ScriptSource { get; set; } = "ai";

    [MaxLength(50)]
    public string ContextType { get; set; } = default!;

    [MaxLength(255)]
    public string? Title { get; set; }

    public string ContentText { get; set; } = default!;

    [MaxLength(20)]
    public string Status { get; set; } = ScriptStatus.Draft.ToString().ToLowerInvariant();

    public Guid? PromptId { get; set; }
    public AiPrompt? Prompt { get; set; }

    public Guid? ParentScriptId { get; set; }
    public Script? ParentScript { get; set; }
    public ICollection<Script> ChildScripts { get; set; } = new List<Script>();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ScriptAudio> Audios { get; set; } = new List<ScriptAudio>();
}

