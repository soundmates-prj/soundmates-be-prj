using AiService.Application.Abstractions.Messaging;

namespace AiService.Application.Features.Scripts.Commands.UpdateScript;

public record UpdateScriptCommand(
    Guid UserId,
    Guid ScriptId,
    string? Title,
    string? ContentText) : ICommand<bool>;
