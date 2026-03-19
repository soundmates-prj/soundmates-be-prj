using AiService.Application.Abstractions.Messaging;
using AiService.Domain.Entities;

namespace AiService.Application.Features.Voices.Queries.GetActiveVoices;

public record GetActiveVoicesQuery : IQuery<IReadOnlyList<TtsVoice>>;

