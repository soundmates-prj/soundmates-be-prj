using AiService.Application.Results;
using AiService.Domain.Entities;

namespace AiService.Application.Interfaces;

public record CreatePromptRequest(
    Guid UserId,
    string ContextType,
    string InputText,
    string? ModelName,
    decimal? Temperature,
    int? MaxTokens);

public interface IPromptService
{
    Task<Result<AiPrompt>> CreateAsync(CreatePromptRequest request, CancellationToken cancellationToken);
}

