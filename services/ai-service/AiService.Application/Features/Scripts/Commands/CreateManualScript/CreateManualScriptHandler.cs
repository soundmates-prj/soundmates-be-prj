using AiService.Application.Abstractions.Messaging;
using AiService.Application.Interfaces;
using AiService.Application.Results;

namespace AiService.Application.Features.Scripts.Commands.CreateManualScript;

public class CreateManualScriptHandler : ICommandHandler<CreateManualScriptCommand, Domain.Entities.Script>
{
    private readonly IScriptService _scripts;

    public CreateManualScriptHandler(IScriptService scripts)
    {
        _scripts = scripts;
    }

    public Task<Result<Domain.Entities.Script>> Handle(CreateManualScriptCommand command, CancellationToken cancellationToken)
        => _scripts.CreateManualAsync(command.UserId, command.ContentText, command.Title, command.Topic, cancellationToken);
}
