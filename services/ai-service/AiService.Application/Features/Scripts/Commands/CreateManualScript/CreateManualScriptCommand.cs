using AiService.Application.Abstractions.Messaging;

namespace AiService.Application.Features.Scripts.Commands.CreateManualScript;

public record CreateManualScriptCommand(
    Guid UserId,
    string ContentText,
    string? Title,
    string? Topic) : ICommand<Domain.Entities.Script>;
