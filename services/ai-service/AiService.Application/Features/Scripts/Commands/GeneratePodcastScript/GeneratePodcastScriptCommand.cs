using AiService.Application.Abstractions.Messaging;
using AiService.Domain.Entities;

namespace AiService.Application.Features.Scripts.Commands.GeneratePodcastScript;

public record GeneratePodcastScriptCommand(
    Guid UserId,
    string Topic,
    string? Title,
    string ContextType,
    string? ModelName,
    decimal? Temperature,
    int? MaxTokens,
    string? EditorInstruction,
    bool UseAutoContext,
    bool StrictFactMode) : ICommand<Script>;

