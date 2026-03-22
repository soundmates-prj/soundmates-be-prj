using AiService.Application.Results;
using AiService.Domain.Entities;

namespace AiService.Application.Interfaces;

public record GeneratePodcastScriptRequest(
    Guid UserId,
    string Topic,
    string? Title,
    string ContextType,
    string? ModelName,
    decimal? Temperature,
    int? MaxTokens,
    string? EditorInstruction = null,
    bool UseAutoContext = true,
    bool StrictFactMode = false);

public record SplitScriptPartsRequest(
    Guid UserId,
    Guid ScriptId,
    int MaxCharsPerPart);

public interface IScriptService
{
    Task<Result<Script>> GeneratePodcastAsync(GeneratePodcastScriptRequest request, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<Script>>> SplitToAudioPartsAsync(SplitScriptPartsRequest request, CancellationToken cancellationToken);
    Task<Result<Script>> GetByIdAsync(Guid scriptId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<Script>>> GetMyScriptsAsync(Guid userId, string? contextType, string? status, CancellationToken cancellationToken);
}

