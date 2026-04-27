using AiService.Application.Abstractions.Messaging;
using AiService.Domain.Entities;

namespace AiService.Application.Features.Audios.Queries.GetAudioById;

public record GetAudioByIdQuery(Guid AudioId) : IQuery<ScriptAudio>;

