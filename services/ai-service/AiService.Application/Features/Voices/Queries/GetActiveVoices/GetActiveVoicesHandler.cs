using AiService.Application.Abstractions.Messaging;
using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Domain.Entities;

namespace AiService.Application.Features.Voices.Queries.GetActiveVoices;

public class GetActiveVoicesHandler : IQueryHandler<GetActiveVoicesQuery, IReadOnlyList<TtsVoice>>
{
    private readonly IVoiceService _voices;

    public GetActiveVoicesHandler(IVoiceService voices)
    {
        _voices = voices;
    }

    public Task<Result<IReadOnlyList<TtsVoice>>> Handle(GetActiveVoicesQuery query, CancellationToken cancellationToken)
        => _voices.GetActiveAsync(cancellationToken);
}

