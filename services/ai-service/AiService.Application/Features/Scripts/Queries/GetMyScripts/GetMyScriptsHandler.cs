using AiService.Application.Abstractions.Messaging;
using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Domain.Entities;

namespace AiService.Application.Features.Scripts.Queries.GetMyScripts;

public class GetMyScriptsHandler : IQueryHandler<GetMyScriptsQuery, IReadOnlyList<Script>>
{
    private readonly IScriptService _scripts;

    public GetMyScriptsHandler(IScriptService scripts)
    {
        _scripts = scripts;
    }

    public Task<Result<IReadOnlyList<Script>>> Handle(GetMyScriptsQuery query, CancellationToken cancellationToken)
        => _scripts.GetMyScriptsAsync(query.UserId, query.ContextType, query.Status, cancellationToken);
}

