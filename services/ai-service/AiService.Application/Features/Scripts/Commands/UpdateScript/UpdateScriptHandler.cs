using AiService.Application.Abstractions.Messaging;
using AiService.Application.Interfaces;
using AiService.Application.Results;

namespace AiService.Application.Features.Scripts.Commands.UpdateScript;

public class UpdateScriptHandler : ICommandHandler<UpdateScriptCommand, bool>
{
    private readonly IScriptService _scripts;

    public UpdateScriptHandler(IScriptService scripts)
    {
        _scripts = scripts;
    }

    public Task<Result<bool>> Handle(UpdateScriptCommand command, CancellationToken cancellationToken)
        => _scripts.UpdateAsync(
            command.UserId,
            command.ScriptId,
            command.Title,
            command.ContentText,
            cancellationToken);
}
