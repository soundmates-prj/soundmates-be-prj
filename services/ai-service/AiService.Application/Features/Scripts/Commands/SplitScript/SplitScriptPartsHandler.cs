using AiService.Application.Abstractions.Messaging;
using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Domain.Entities;

namespace AiService.Application.Features.Scripts.Commands.SplitScript;

public class SplitScriptPartsHandler : ICommandHandler<SplitScriptPartsCommand, IReadOnlyList<Script>>
{
    private readonly IScriptService _scripts;

    public SplitScriptPartsHandler(IScriptService scripts)
    {
        _scripts = scripts;
    }

    public Task<Result<IReadOnlyList<Script>>> Handle(SplitScriptPartsCommand command, CancellationToken cancellationToken)
        => _scripts.SplitToAudioPartsAsync(
            new SplitScriptPartsRequest(command.UserId, command.ScriptId, command.MaxCharsPerPart),
            cancellationToken);
}

