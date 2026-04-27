using AiService.Application.Abstractions.Messaging;
using AiService.Application.Interfaces;
using AiService.Application.Results;

namespace AiService.Application.Features.Scripts.Commands.DeleteScript;

public class DeleteScriptHandler : ICommandHandler<DeleteScriptCommand, bool>
{
    private readonly IScriptService _scripts;

    public DeleteScriptHandler(IScriptService scripts)
    {
        _scripts = scripts;
    }

    public Task<Result<bool>> Handle(DeleteScriptCommand command, CancellationToken cancellationToken)
        => _scripts.DeleteAsync(command.UserId, command.ScriptId, cancellationToken);
}
