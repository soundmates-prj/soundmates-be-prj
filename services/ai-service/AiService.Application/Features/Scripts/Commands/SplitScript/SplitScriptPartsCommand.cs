using AiService.Application.Abstractions.Messaging;
using AiService.Domain.Entities;

namespace AiService.Application.Features.Scripts.Commands.SplitScript;

public record SplitScriptPartsCommand(
    Guid UserId,
    Guid ScriptId,
    int MaxCharsPerPart) : ICommand<IReadOnlyList<Script>>;

