using AiService.Application.Abstractions.Messaging;
using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Domain.Entities;

namespace AiService.Application.Features.Audios.Queries.GetAudioById;

public class GetAudioByIdHandler : IQueryHandler<GetAudioByIdQuery, ScriptAudio>
{
    private readonly IAudioService _audios;

    public GetAudioByIdHandler(IAudioService audios)
    {
        _audios = audios;
    }

    public Task<Result<ScriptAudio>> Handle(GetAudioByIdQuery query, CancellationToken cancellationToken)
        => _audios.GetByIdAsync(query.AudioId, cancellationToken);
}

