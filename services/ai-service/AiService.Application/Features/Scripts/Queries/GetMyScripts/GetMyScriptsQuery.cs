using AiService.Application.Abstractions.Messaging;
using AiService.Domain.Entities;

namespace AiService.Application.Features.Scripts.Queries.GetMyScripts;

public record GetMyScriptsQuery(
    Guid UserId,
    string? ContextType,
    string? Status) : IQuery<IReadOnlyList<Script>>;

