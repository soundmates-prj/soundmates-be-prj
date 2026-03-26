using MediatR;

namespace AccountContentService.Application.Features.SystemSettings.Queries.GetGeminiConfig;

public sealed record GetGeminiConfigQuery : IRequest<GetGeminiConfigResult?>;
