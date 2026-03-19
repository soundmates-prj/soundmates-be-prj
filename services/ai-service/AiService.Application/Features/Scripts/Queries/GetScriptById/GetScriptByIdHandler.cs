using AiService.Application.Abstractions.Messaging;
using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Domain.Entities;

namespace AiService.Application.Features.Scripts.Queries.GetScriptById;

public class GetScriptByIdHandler : IQueryHandler<GetScriptByIdQuery, Script>
{
    private readonly IScriptService _scripts;

    public GetScriptByIdHandler(IScriptService scripts)
    {
        _scripts = scripts;
    }

    public Task<Result<Script>> Handle(GetScriptByIdQuery query, CancellationToken cancellationToken)
        => _scripts.GetByIdAsync(query.ScriptId, cancellationToken);
}

