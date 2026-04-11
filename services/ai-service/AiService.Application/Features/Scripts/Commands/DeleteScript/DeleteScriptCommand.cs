using AiService.Application.Abstractions.Messaging;

namespace AiService.Application.Features.Scripts.Commands.DeleteScript;

public record DeleteScriptCommand(Guid UserId, Guid ScriptId) : ICommand<bool>;
