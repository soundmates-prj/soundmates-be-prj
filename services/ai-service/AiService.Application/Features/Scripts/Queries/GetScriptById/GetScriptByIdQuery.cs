using AiService.Application.Abstractions.Messaging;
using AiService.Domain.Entities;

namespace AiService.Application.Features.Scripts.Queries.GetScriptById;

public record GetScriptByIdQuery(Guid ScriptId) : IQuery<Script>;

